using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.ExpirarReservasCaducadas;

/// <summary>
/// Expires pending bookings past their <c>ExpiraEn</c>. Their seats are already free (occupancy ignores
/// them); this makes the state coherent and triggers the email. Each booking is saved on its own with its
/// <c>xmin</c>, so a booking the client confirms at the same time is skipped instead of overwritten.
/// </summary>
public class ExpirarReservasCaducadasHandler(
    IAppDbContext dbContext,
    IStripeGateway stripe,
    IPublisher publisher,
    TimeProvider timeProvider,
    ILogger<ExpirarReservasCaducadasHandler> logger)
    : IRequestHandler<ExpirarReservasCaducadasCommand, Result<int>>
{
    public const int TamanoLote = 100;

    /// <summary>Advisory lock key: only one replica of the API runs a batch at a time.</summary>
    public const long ClaveLock = 0x5A4F_4900_0001;

    public async Task<Result<int>> Handle(ExpirarReservasCaducadasCommand request, CancellationToken cancellationToken)
    {
        await using var bloqueo = await dbContext.TryAdquirirLockAsync(ClaveLock, cancellationToken);
        if (bloqueo is null)
        {
            logger.LogInformation("Otra instancia está expirando reservas; se salta este ciclo.");
            return Result.Success(0);
        }

        var now = timeProvider.GetUtcNow();
        var ids = await dbContext.Reservas
            .Where(r => r.Estado == EstadoReserva.Pendiente && r.ExpiraEn <= now)
            .OrderBy(r => r.ExpiraEn)
            .Select(r => r.Id)
            .Take(TamanoLote)
            .ToListAsync(cancellationToken);

        var expiradas = 0;
        foreach (var id in ids)
        {
            try
            {
                if (await ExpirarAsync(id, now, cancellationToken))
                {
                    expiradas++;
                    await publisher.PublicarTrasCommitAsync(new ReservaExpiradaEvent(id), logger, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One booking (a concurrent confirmation, Stripe down…) must not stop the batch.
                logger.LogWarning(ex, "No se pudo expirar la reserva {ReservaId}; se reintentará en el próximo ciclo.", id);
                dbContext.DescartarCambios();
            }
        }

        return Result.Success(expiradas);
    }

    private async Task<bool> ExpirarAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.Reservas.FirstOrDefaultAsync(
            r => r.Id == id && r.Estado == EstadoReserva.Pendiente && r.ExpiraEn <= now, cancellationToken);
        if (reserva is null)
        {
            return false;
        }

        if (reserva.Metodo == MetodoPago.Stripe
            && reserva.StripeSessionId is { } sessionId
            && await stripe.ExpirarSesionAsync(sessionId, cancellationToken) == ResultadoExpiracionSesion.YaCompletada)
        {
            // Paid in Stripe: the "completed" webhook will confirm it.
            return false;
        }

        reserva.Expirar(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

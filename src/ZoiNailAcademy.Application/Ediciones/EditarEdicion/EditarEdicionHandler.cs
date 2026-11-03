using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Events;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones.EditarEdicion;

public class EditarEdicionHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IPublisher publisher, IReservasSettings settings,
    IStripeGateway stripe, TimeProvider timeProvider,
    ILogger<EditarEdicionHandler> logger)
    : IRequestHandler<EditarEdicionCommand, Result<EdicionEditadaDto>>
{
    public const string ConcurrenciaMessage =
        "Otra persona ha modificado esta edición mientras la editabas. Recárgala y vuelve a intentarlo.";

    public async Task<Result<EdicionEditadaDto>> Handle(EditarEdicionCommand request, CancellationToken cancellationToken)
    {
        var edicion = await dbContext.Ediciones
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Edición", request.Id);

        if (!uint.TryParse(request.Version, out var version) || version != edicion.Version)
        {
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }

        var ocupaciones = await ocupacionReader.LeerAsync([edicion.Id], cancellationToken);
        var ocupacion = Ocupacion.De(ocupaciones, edicion.Id);
        var now = timeProvider.GetUtcNow();

        var cambio = edicion.Actualizar(
            request.Inicio, request.Plazas, request.Estado, request.Notas, ocupacion.Total,
            request.ConfirmarCambioFecha, now, pagadas: ocupacion.Pagadas);

        // Cancelling the edition cancels its pending bookings in the same save; paid ones are refunded by hand.
        var pendientesCanceladas = cambio.Cancelada
            ? await dbContext.Reservas
                .Where(r => r.EdicionId == edicion.Id && r.Estado == EstadoReserva.Pendiente)
                .ToListAsync(cancellationToken)
            : [];
        foreach (var reserva in pendientesCanceladas)
        {
            reserva.Cancelar(now);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // xmin changed between our read and our write: someone else saved the edition first.
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }

        if (cambio.Publicada)
        {
            await publisher.PublicarTrasCommitAsync(new EdicionPublicadaEvent(edicion.Id), logger, cancellationToken);
        }

        foreach (var reserva in pendientesCanceladas)
        {
            await ExpirarSesionStripeAsync(reserva, cancellationToken);
            await publisher.PublicarTrasCommitAsync(new ReservaCanceladaEvent(reserva.Id), logger, cancellationToken);
        }

        return Result.Success(new EdicionEditadaDto(
            EdicionAdminDto.From(edicion, ocupacion, now, settings.CierreAntesDeInicio), cambio.Avisos));
    }

    // After the commit: a session left open could still be paid (it would arrive as a late payment).
    private async Task ExpirarSesionStripeAsync(Reserva reserva, CancellationToken cancellationToken)
    {
        if (reserva is not { Metodo: MetodoPago.Stripe, StripeSessionId: { } sessionId })
        {
            return;
        }

        try
        {
            await stripe.ExpirarSesionAsync(sessionId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "No se pudo expirar la sesión de Stripe de la reserva {Codigo}.", reserva.Codigo);
        }
    }
}

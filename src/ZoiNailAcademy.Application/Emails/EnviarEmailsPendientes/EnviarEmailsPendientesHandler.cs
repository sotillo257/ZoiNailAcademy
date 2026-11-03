using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Emails.EnviarEmailsPendientes;

/// <summary>
/// Sends up to <see cref="TamanoLote"/> pending emails whose next attempt is due, oldest first. A failure is
/// retried with backoff and the email becomes <c>Fallido</c> after <c>EmailSaliente.MaxIntentos</c>. Runs behind
/// an advisory lock, so several replicas never send the same email twice.
/// </summary>
public class EnviarEmailsPendientesHandler(
    IAppDbContext dbContext, IEmailSender sender, TimeProvider timeProvider, ILogger<EnviarEmailsPendientesHandler> logger)
    : IRequestHandler<EnviarEmailsPendientesCommand, Result<int>>
{
    public const int TamanoLote = 20;

    /// <summary>Advisory lock key (the expiry job of F04 uses <c>…0001</c>).</summary>
    public const long ClaveLock = 0x5A4F_4900_0002;

    public async Task<Result<int>> Handle(EnviarEmailsPendientesCommand request, CancellationToken cancellationToken)
    {
        await using var bloqueo = await dbContext.TryAdquirirLockAsync(ClaveLock, cancellationToken);
        if (bloqueo is null)
        {
            logger.LogInformation("Otra instancia está enviando emails; se salta este ciclo.");
            return Result.Success(0);
        }

        var now = timeProvider.GetUtcNow();
        var ids = await dbContext.EmailsSalientes
            .Where(e => e.Estado == EstadoEmail.Pendiente && e.ProximoIntentoEn <= now)
            .OrderBy(e => e.ProximoIntentoEn)
            .Select(e => e.Id)
            .Take(TamanoLote)
            .ToListAsync(cancellationToken);

        var enviados = 0;
        foreach (var id in ids)
        {
            try
            {
                if (await EnviarAsync(id, cancellationToken))
                {
                    enviados++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Saving the result failed (database): the email stays pending and is retried next cycle.
                logger.LogError(ex, "No se pudo registrar el envío del email {EmailId}.", id);
                dbContext.DescartarCambios();
            }
        }

        return Result.Success(enviados);
    }

    private async Task<bool> EnviarAsync(Guid id, CancellationToken cancellationToken)
    {
        var email = await dbContext.EmailsSalientes.FirstOrDefaultAsync(
            e => e.Id == id && e.Estado == EstadoEmail.Pendiente, cancellationToken);
        if (email is null)
        {
            return false;
        }

        bool enviado;
        try
        {
            var proveedorId = await sender.EnviarAsync(
                new(email.Id, email.Para, email.ReplyTo, email.Asunto, email.Html, email.Texto, email.Tipo, email.BajaUrl), cancellationToken);
            email.MarcarEnviado(proveedorId, timeProvider.GetUtcNow());
            enviado = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (email.RegistrarFallo(ex.Message, timeProvider.GetUtcNow()))
            {
                logger.LogError(ex, "El email {Tipo} {EmailId} ha fallado {Intentos} veces; se da por fallido.",
                    email.Tipo, email.Id, email.Intentos);
            }
            else
            {
                logger.LogWarning(ex, "Falló el envío del email {Tipo} {EmailId} (intento {Intentos}); se reintentará el {Proximo}.",
                    email.Tipo, email.Id, email.Intentos, email.ProximoIntentoEn);
            }

            enviado = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return enviado;
    }
}

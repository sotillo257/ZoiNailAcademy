using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Emails;

/// <summary>
/// Enqueues emails in the outbox (<see cref="EmailSaliente"/>); <c>EnviarEmailsJob</c> sends them. An email with a
/// reference is enqueued once per (type, reference) even if its event is published twice.
/// </summary>
public class BandejaSalida(
    IAppDbContext dbContext, IAppSettings appSettings, IPagoSettings pagoSettings, IEmailSettings emailSettings,
    TimeProvider timeProvider, ILogger<BandejaSalida> logger)
{
    public EmailMarca Marca() => new(
        appSettings.NombreAcademia, appSettings.DireccionAcademia, appSettings.BaseUrl, appSettings.TimeZone,
        emailSettings.ReplyTo, pagoSettings.WhatsAppTelefono);

    /// <summary>Enqueues a notice for the client; skipped (with a warning) while <c>Email:AvisosClienta</c> is empty.</summary>
    public Task<bool> EncolarParaClientaAsync(
        string tipo, EmailContenido contenido, Guid? referenciaId, CancellationToken cancellationToken, string? replyTo = null)
    {
        if (string.IsNullOrWhiteSpace(emailSettings.AvisosClienta))
        {
            logger.LogWarning("Email:AvisosClienta no está configurado; no se encola el aviso {Tipo}.", tipo);
            return Task.FromResult(false);
        }

        return EncolarAsync(tipo, emailSettings.AvisosClienta, replyTo, contenido, referenciaId, cancellationToken);
    }

    /// <summary>Enqueues an email for a buyer or sender, with the client's address as Reply-To.</summary>
    public Task<bool> EncolarParaClienteAsync(
        string tipo, string para, EmailContenido contenido, Guid? referenciaId, CancellationToken cancellationToken) =>
        EncolarAsync(tipo, para, emailSettings.ReplyTo, contenido, referenciaId, cancellationToken);

    /// <returns>False when the email was already enqueued.</returns>
    public async Task<bool> EncolarAsync(
        string tipo, string para, string? replyTo, EmailContenido contenido, Guid? referenciaId,
        CancellationToken cancellationToken)
    {
        if (referenciaId is { } referencia
            && await dbContext.EmailsSalientes.AnyAsync(e => e.Tipo == tipo && e.ReferenciaId == referencia, cancellationToken))
        {
            logger.LogInformation("El email {Tipo} de {ReferenciaId} ya estaba encolado.", tipo, referencia);
            return false;
        }

        var email = EmailSaliente.Create(
            para, replyTo, contenido.Asunto, contenido.Html, contenido.Texto, tipo, referenciaId, timeProvider.GetUtcNow());
        dbContext.EmailsSalientes.Add(email);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (referenciaId is not null)
        {
            // The same event handled twice at the same time: the unique index (Tipo, ReferenciaId) kept the first one.
            logger.LogInformation(ex, "El email {Tipo} de {ReferenciaId} se encoló a la vez en otra petición.", tipo, referenciaId);
            dbContext.EmailsSalientes.Remove(email);
            return false;
        }
    }
}

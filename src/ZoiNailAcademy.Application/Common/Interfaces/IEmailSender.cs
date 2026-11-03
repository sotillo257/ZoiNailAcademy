namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>An email of the outbox, ready to hand to the provider.</summary>
/// <param name="Id">Outbox id, used as the provider's idempotency key.</param>
public sealed record EmailMensaje(Guid Id, string Para, string? ReplyTo, string Asunto, string Html, string Texto, string Tipo);

/// <summary>Sends one email (Resend, or the log when Resend is not configured). Only the outbox job calls it.</summary>
public interface IEmailSender
{
    /// <returns>Id given by the provider.</returns>
    /// <exception cref="Exception">Any failure: the job retries with backoff.</exception>
    Task<string?> EnviarAsync(EmailMensaje mensaje, CancellationToken cancellationToken);
}

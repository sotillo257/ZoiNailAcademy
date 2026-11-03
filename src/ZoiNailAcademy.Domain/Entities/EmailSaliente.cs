using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Domain.Entities;

/// <summary>
/// An email in the outbox. Domain events enqueue it and a job sends it with retries, so a provider outage
/// never breaks a booking or a webhook. Sent emails are deleted after <see cref="DiasRetencion"/> days (GDPR).
/// </summary>
public class EmailSaliente : BaseAuditableEntity
{
    public const int EmailMaxLength = 200;
    public const int AsuntoMaxLength = 300;
    public const int TipoMaxLength = 50;
    public const int UltimoErrorMaxLength = 1000;
    public const int ProveedorIdMaxLength = 100;
    public const int DiasRetencion = 90;

    /// <summary>After this many failed attempts the email is <see cref="EstadoEmail.Fallido"/>.</summary>
    public const int MaxIntentos = 5;

    /// <summary>
    /// Wait after the n-th failed attempt (1 min, 5 min, 15 min, 1 h). The spec also lists 4 h, but it would
    /// only apply after the 5th failure, which is final.
    /// </summary>
    public static readonly IReadOnlyList<TimeSpan> Backoff =
    [
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1)
    ];

    public string Para { get; private set; } = null!;
    public string? ReplyTo { get; private set; }
    public string Asunto { get; private set; } = null!;
    public string Html { get; private set; } = null!;
    public string Texto { get; private set; } = null!;

    /// <summary>Template, e.g. <c>ReservaPagada.Comprador</c>.</summary>
    public string Tipo { get; private set; } = null!;

    /// <summary>Booking, waiting list entry… With <see cref="Tipo"/> it is unique: an event published twice enqueues one email.</summary>
    public Guid? ReferenciaId { get; private set; }

    public EstadoEmail Estado { get; private set; }
    public int Intentos { get; private set; }
    public DateTimeOffset ProximoIntentoEn { get; private set; }
    public string? UltimoError { get; private set; }

    /// <summary>Id given by the provider (Resend), or <c>log</c> when it was only written to the log.</summary>
    public string? ProveedorId { get; private set; }

    public DateTimeOffset? EnviadoEn { get; private set; }

    private EmailSaliente()
    {
    }

    public static EmailSaliente Create(
        string para, string? replyTo, string asunto, string html, string texto, string tipo, Guid? referenciaId,
        DateTimeOffset now)
    {
        var asuntoLimpio = Required(asunto, "El asunto es obligatorio.");
        return new EmailSaliente
        {
            Id = Guid.NewGuid(),
            Para = Required(para, "El destinatario es obligatorio."),
            ReplyTo = string.IsNullOrWhiteSpace(replyTo) ? null : replyTo.Trim(),
            Asunto = asuntoLimpio.Length <= AsuntoMaxLength ? asuntoLimpio : asuntoLimpio[..AsuntoMaxLength],
            Html = Required(html, "El HTML del email es obligatorio."),
            Texto = Required(texto, "El texto del email es obligatorio."),
            Tipo = Required(tipo, "El tipo de email es obligatorio."),
            ReferenciaId = referenciaId,
            Estado = EstadoEmail.Pendiente,
            ProximoIntentoEn = now
        };
    }

    public void MarcarEnviado(string? proveedorId, DateTimeOffset now)
    {
        EnsurePendiente();
        Estado = EstadoEmail.Enviado;
        Intentos++;
        EnviadoEn = now;
        ProveedorId = proveedorId is { Length: > ProveedorIdMaxLength } ? proveedorId[..ProveedorIdMaxLength] : proveedorId;
        UltimoError = null;
    }

    /// <returns>True when this was the last attempt and the email is now <see cref="EstadoEmail.Fallido"/>.</returns>
    public bool RegistrarFallo(string error, DateTimeOffset now)
    {
        EnsurePendiente();
        Intentos++;
        UltimoError = error.Length <= UltimoErrorMaxLength ? error : error[..UltimoErrorMaxLength];
        if (Intentos >= MaxIntentos)
        {
            Estado = EstadoEmail.Fallido;
            return true;
        }

        ProximoIntentoEn = now + Backoff[Intentos - 1];
        return false;
    }

    private void EnsurePendiente()
    {
        if (Estado != EstadoEmail.Pendiente)
        {
            throw new DomainException($"Un email {Estado} no se puede volver a enviar.");
        }
    }

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException(message) : value.Trim();
}

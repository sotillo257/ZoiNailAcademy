namespace ZoiNailAcademy.Domain.Entities;

/// <summary>A Stripe webhook event already processed (idempotency: Stripe may deliver an event more than once).</summary>
public class StripeEvento
{
    public const int EventIdMaxLength = 100;
    public const int TipoMaxLength = 100;

    public string EventId { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public DateTimeOffset ProcesadoEn { get; private set; }

    private StripeEvento()
    {
    }

    public static StripeEvento Create(string eventId, string tipo, DateTimeOffset now) => new()
    {
        EventId = eventId,
        Tipo = tipo,
        ProcesadoEn = now
    };
}

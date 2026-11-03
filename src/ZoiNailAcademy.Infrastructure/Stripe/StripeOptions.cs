namespace ZoiNailAcademy.Infrastructure.Stripe;

/// <summary>Section <c>Stripe</c>. Both values are secrets: only environment variables, never committed.</summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Signing secret of the webhook endpoint (<c>whsec_…</c>).</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}

namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Verifies the <c>Stripe-Signature</c> of a webhook body and extracts what the API needs.</summary>
public interface IStripeWebhookParser
{
    /// <exception cref="Exceptions.ValidationException">The signature is missing or invalid.</exception>
    StripeWebhookEvento Parse(string json, string? signatureHeader);
}

/// <param name="SessionId">Checkout session id, when the event is about a session.</param>
/// <param name="ClientReferenceId">The booking id sent when creating the session.</param>
/// <param name="PaymentStatus"><c>paid</c>, <c>unpaid</c> or <c>no_payment_required</c>.</param>
public sealed record StripeWebhookEvento(
    string Id, string Tipo, string? SessionId, string? ClientReferenceId, string? PaymentStatus, string? PaymentIntentId)
{
    public const string CheckoutCompletado = "checkout.session.completed";
    public const string CheckoutExpirado = "checkout.session.expired";
}

using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using ZoiNailAcademy.Application.Common.Interfaces;
using ValidationException = ZoiNailAcademy.Application.Common.Exceptions.ValidationException;

namespace ZoiNailAcademy.Infrastructure.Stripe;

public class StripeWebhookParser(IOptions<StripeOptions> options) : IStripeWebhookParser
{
    /// <summary>Maximum age of a signed event (Stripe's default), against replay attacks.</summary>
    public const long ToleranciaSegundos = 300;

    public StripeWebhookEvento Parse(string json, string? signatureHeader)
    {
        Event stripeEvent;
        try
        {
            // The API version of the account may differ from the SDK's; only the fields we read matter.
            stripeEvent = EventUtility.ConstructEvent(
                json, signatureHeader ?? string.Empty, options.Value.WebhookSecret, ToleranciaSegundos,
                throwOnApiVersionMismatch: false);
        }
        catch (Exception ex) when (ex is StripeException or ArgumentException or Newtonsoft.Json.JsonException)
        {
            throw new ValidationException([new ValidationFailure("Stripe-Signature", "La firma del webhook no es válida.")]);
        }

        var session = stripeEvent.Data?.Object as Session;
        return new StripeWebhookEvento(
            stripeEvent.Id, stripeEvent.Type, session?.Id, session?.ClientReferenceId, session?.PaymentStatus,
            session?.PaymentIntentId);
    }
}

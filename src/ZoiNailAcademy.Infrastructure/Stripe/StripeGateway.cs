using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Stripe;

/// <summary>Stripe Checkout through Stripe.net. Card data never reaches the API: Stripe hosts the payment page.</summary>
public class StripeGateway(IOptions<StripeOptions> options, ILogger<StripeGateway> logger) : IStripeGateway
{
    private readonly Lazy<SessionService> _sessions = new(() => new SessionService(new StripeClient(options.Value.SecretKey)));

    public async Task<CheckoutSesion> CrearSesionCheckoutAsync(CheckoutSesionRequest request, CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["reservaId"] = request.ReservaId.ToString(),
            ["codigo"] = request.Codigo
        };

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            Currency = "eur",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = request.ImporteCentimos,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Concepto }
                    }
                }
            ],
            CustomerEmail = request.Email,
            ClientReferenceId = request.ReservaId.ToString(),
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = new Dictionary<string, string>(metadata) },
            ExpiresAt = request.ExpiraEn.UtcDateTime,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Locale = "es"
        };

        try
        {
            var session = await _sessions.Value.CreateAsync(
                sessionOptions, new RequestOptions { IdempotencyKey = request.IdempotencyKey }, cancellationToken);
            return new CheckoutSesion(session.Id, session.Url);
        }
        catch (Exception ex) when (ex is StripeException or HttpRequestException)
        {
            throw new PaymentProviderException($"Stripe no pudo crear la sesión de la reserva {request.Codigo}.", ex);
        }
    }

    public async Task<ResultadoExpiracionSesion> ExpirarSesionAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await _sessions.Value.ExpireAsync(sessionId, cancellationToken: cancellationToken);
            return ResultadoExpiracionSesion.Expirada;
        }
        catch (StripeException ex)
        {
            // Only open sessions can be expired: find out whether it was paid or had already expired.
            logger.LogInformation(ex, "No se pudo expirar la sesión {SessionId}; se consulta su estado.", sessionId);
            return await EstadoActualAsync(sessionId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentProviderException($"No se pudo contactar con Stripe para expirar {sessionId}.", ex);
        }
    }

    private async Task<ResultadoExpiracionSesion> EstadoActualAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.Value.GetAsync(sessionId, cancellationToken: cancellationToken);
            return session.Status switch
            {
                "complete" => ResultadoExpiracionSesion.YaCompletada,
                "expired" => ResultadoExpiracionSesion.Expirada,
                _ => throw new PaymentProviderException($"La sesión {sessionId} sigue abierta y no se pudo expirar.")
            };
        }
        catch (Exception ex) when (ex is StripeException or HttpRequestException)
        {
            throw new PaymentProviderException($"No se pudo consultar la sesión {sessionId} en Stripe.", ex);
        }
    }
}

namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Network calls to Stripe. Implemented with Stripe.net in Infrastructure; faked in integration tests.</summary>
public interface IStripeGateway
{
    /// <exception cref="Exceptions.PaymentProviderException">Stripe could not create the session.</exception>
    Task<CheckoutSesion> CrearSesionCheckoutAsync(CheckoutSesionRequest request, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.PaymentProviderException">Stripe could not be reached.</exception>
    Task<ResultadoExpiracionSesion> ExpirarSesionAsync(string sessionId, CancellationToken cancellationToken);
}

/// <param name="ImporteCentimos">Amount in euro cents.</param>
/// <param name="IdempotencyKey">Stripe idempotency key, so a retried request does not create two sessions.</param>
public sealed record CheckoutSesionRequest(
    Guid ReservaId, string Codigo, string Concepto, long ImporteCentimos, string Email, DateTimeOffset ExpiraEn,
    string SuccessUrl, string CancelUrl, string IdempotencyKey);

public sealed record CheckoutSesion(string Id, string Url);

public enum ResultadoExpiracionSesion
{
    /// <summary>The session is expired now (or already was): no payment can happen through it.</summary>
    Expirada,

    /// <summary>The session was already paid: the webhook will confirm the booking.</summary>
    YaCompletada
}

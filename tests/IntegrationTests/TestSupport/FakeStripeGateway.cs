using System.Collections.Concurrent;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace IntegrationTests.TestSupport;

/// <summary>
/// Stripe without network. Creating a session fails when the buyer's email contains
/// <see cref="EmailQueFalla"/>; a session marked with <see cref="MarcarCompletada"/> cannot be expired.
/// </summary>
public class FakeStripeGateway : IStripeGateway
{
    public const string EmailQueFalla = "stripe-falla";

    private readonly ConcurrentDictionary<string, CheckoutSesionRequest> _sesiones = new();
    private readonly ConcurrentDictionary<string, bool> _completadas = new();
    private readonly ConcurrentDictionary<string, bool> _expiradas = new();

    public CheckoutSesionRequest Sesion(string sessionId) => _sesiones[sessionId];

    public bool FueExpirada(string sessionId) => _expiradas.ContainsKey(sessionId);

    public void MarcarCompletada(string sessionId) => _completadas[sessionId] = true;

    public Task<CheckoutSesion> CrearSesionCheckoutAsync(CheckoutSesionRequest request, CancellationToken cancellationToken)
    {
        if (request.Email.Contains(EmailQueFalla, StringComparison.Ordinal))
        {
            throw new PaymentProviderException("Fallo simulado de Stripe.");
        }

        var id = $"cs_test_{Guid.NewGuid():N}";
        _sesiones[id] = request;
        return Task.FromResult(new CheckoutSesion(id, $"https://checkout.stripe.test/{id}"));
    }

    public Task<ResultadoExpiracionSesion> ExpirarSesionAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (_completadas.ContainsKey(sessionId))
        {
            return Task.FromResult(ResultadoExpiracionSesion.YaCompletada);
        }

        _expiradas[sessionId] = true;
        return Task.FromResult(ResultadoExpiracionSesion.Expirada);
    }
}

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Controllers.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>Helpers for the booking and Stripe webhook tests.</summary>
public abstract class ReservasTestBase(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    /// <summary>PostgreSQL stores microseconds; the test clock has 100 ns ticks.</summary>
    protected static readonly TimeSpan Precision = TimeSpan.FromMilliseconds(1);

    protected static object Formulario(Guid edicionId, string? email = null, string? telefono = null, string? website = null,
        bool aceptaCondiciones = true) => new
    {
        edicionId,
        nombre = "Lucía Pérez",
        email = email ?? $"{Guid.NewGuid():N}@mail.test",
        telefono = telefono ?? UniqueTelefono(),
        aceptaCondiciones,
        website = website ?? ""
    };

    protected Task<HttpResponseMessage> ReservarAsync(object formulario) =>
        SendAsync(HttpMethod.Post, "/api/reservas/tarjeta", content: JsonContent.Create(formulario));

    protected Task<HttpResponseMessage> ReservarManualAsync(object formulario, HttpClient? client = null) =>
        SendAsync(HttpMethod.Post, "/api/reservas/manual", content: JsonContent.Create(formulario), client: client);

    protected async Task<ReservaPublicaTestDto> ReservarManualOkAsync(Guid edicionId, HttpClient? client = null)
    {
        var response = await ReservarManualAsync(Formulario(edicionId), client);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadAsync<ReservaPublicaTestDto>(response);
    }

    /// <summary>Changes a booking directly in the database (e.g. to expire it without the job).</summary>
    protected async Task ModificarReservaAsync(string codigo, Action<Reserva> cambio)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        cambio(await db.Reservas.SingleAsync(r => r.Codigo == codigo));
        await db.SaveChangesAsync();
    }

    protected async Task<string> VersionAsync(string codigo) =>
        (await BuscarReservaAsync(codigo))!.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);

    protected async Task<ReservaTarjetaTestDto> ReservarOkAsync(Guid edicionId)
    {
        var response = await ReservarAsync(Formulario(edicionId));
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadAsync<ReservaTarjetaTestDto>(response);
    }

    protected async Task<Edicion> CrearEdicionReservableAsync(int plazas = 10, decimal? precio = null)
    {
        var curso = await CrearCursoEnBdAsync();
        return await CrearEdicionEnBdAsync(curso, Now.AddDays(20), plazas);
    }

    protected async Task<Reserva?> BuscarReservaAsync(string codigo)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Reservas.AsNoTracking().SingleOrDefaultAsync(r => r.Codigo == codigo);
    }

    protected async Task<int> ContarReservasAsync(Guid edicionId)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Reservas.CountAsync(r => r.EdicionId == edicionId);
    }

    /// <summary>Sends a Checkout event signed like Stripe does (HMAC-SHA256 of "{timestamp}.{payload}").</summary>
    protected Task<HttpResponseMessage> EnviarWebhookAsync(
        string tipo, string sessionId, string? clientReferenceId = null, string paymentStatus = "paid",
        string? eventId = null, string secret = CustomWebApplicationFactory.StripeWebhookSecret)
    {
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId ?? $"evt_{Guid.NewGuid():N}",
            @object = "event",
            api_version = "2025-01-27.acacia",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            pending_webhooks = 1,
            type = tipo,
            data = new
            {
                @object = new
                {
                    id = sessionId,
                    @object = "checkout.session",
                    client_reference_id = clientReferenceId,
                    payment_status = paymentStatus,
                    status = tipo == "checkout.session.expired" ? "expired" : "complete",
                    payment_intent = "pi_test_123"
                }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", Firmar(payload, secret));
        return Client.SendAsync(request);
    }

    // Stripe signs with the real clock (300 s tolerance), not the host's fake one.
    private static string Firmar(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }
}

public record ReservaTarjetaTestDto(string Codigo, string CheckoutUrl);

public record DatosPagoTestDto(string Titular, string Iban, string BizumTelefono, string Concepto);

public record ReservaPublicaTestDto(
    string Codigo, string Estado, string Metodo, string Curso, DateTimeOffset Inicio, decimal Importe,
    DateTimeOffset? ExpiraEn, DatosPagoTestDto? DatosPago, string? WhatsappUrl);

public record ReservaAdminTestDto(
    Guid Id, string Codigo, Guid EdicionId, string Curso, DateTimeOffset Inicio, string Metodo, string Estado,
    string Nombre, string Email, string Telefono, decimal Importe, DateTimeOffset CreadaEn, DateTimeOffset? PagadaEn,
    DateTimeOffset ExpiraEn, bool RequiereRevision, string? NotaInterna, string? StripePaymentIntentId, string Version);

public record ReservaAccionTestDto(ReservaAdminTestDto Reserva, List<string> Avisos);

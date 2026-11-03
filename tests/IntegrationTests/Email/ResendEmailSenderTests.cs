using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Infrastructure.Email;

namespace IntegrationTests.Email;

/// <summary>Request sent to Resend (HTTP stubbed: no network).</summary>
public class ResendEmailSenderTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"id":"re_123"}""") };
        }
    }

    private static async Task<(JsonElement Body, HttpRequestMessage Request, string? Id)> EnviarAsync(EmailMensaje mensaje)
    {
        var handler = new CapturingHandler();
        var sender = new ResendEmailSender(
            new HttpClient(handler) { BaseAddress = ResendEmailSender.BaseAddress },
            Options.Create(new EmailOptions { From = "Zoi <reservas@zoi.test>" }),
            Options.Create(new ResendOptions { ApiKey = "re_test" }));

        var id = await sender.EnviarAsync(mensaje, CancellationToken.None);
        return (JsonDocument.Parse(handler.Body!).RootElement, handler.Request!, id);
    }

    [Fact]
    public async Task EnviarAsync_WithUnsubscribeUrl_SendsTheOneClickHeaders()
    {
        var mensaje = new EmailMensaje(
            Guid.NewGuid(), "lucia@mail.test", "zoi@academia.test", "Nuevas fechas", "<p>Hola</p>", "Hola", "AvisoNuevasEdiciones",
            "https://web.test/api/lista-espera/baja?token=abc");

        var (body, request, id) = await EnviarAsync(mensaje);

        id.Should().Be("re_123");
        request.Headers.GetValues("Idempotency-Key").Should().Equal(mensaje.Id.ToString());
        var headers = body.GetProperty("headers");
        headers.GetProperty("List-Unsubscribe").GetString().Should().Be("<https://web.test/api/lista-espera/baja?token=abc>");
        headers.GetProperty("List-Unsubscribe-Post").GetString().Should().Be("List-Unsubscribe=One-Click");
    }

    [Fact]
    public async Task EnviarAsync_TransactionalEmail_SendsNoHeaders()
    {
        var (body, _, _) = await EnviarAsync(new EmailMensaje(
            Guid.NewGuid(), "lucia@mail.test", null, "Reserva confirmada", "<p>Hola</p>", "Hola", "ReservaPagada.Comprador"));

        body.TryGetProperty("headers", out _).Should().BeFalse();
        body.TryGetProperty("reply_to", out _).Should().BeFalse();
    }
}

using System.Net;
using FluentAssertions;
using IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>POST /api/stripe/webhook with payloads signed like Stripe does.</summary>
public class StripeWebhookTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    private async Task<(ReservaTarjetaTestDto Creada, string SessionId, Guid ReservaId)> ReservaPendienteAsync(Guid edicionId)
    {
        var creada = await ReservarOkAsync(edicionId);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        return (creada, reserva.StripeSessionId!, reserva.Id);
    }

    private async Task MarcarAsync(string codigo, EstadoReserva estado)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reserva = db.Reservas.Single(r => r.Codigo == codigo);
        if (estado == EstadoReserva.Expirada) reserva.Expirar(Now); else reserva.Cancelar(Now);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Webhook_InvalidSignature_Returns400AndChangesNothing()
    {
        var edicion = await CrearEdicionReservableAsync();
        var (creada, sesion, _) = await ReservaPendienteAsync(edicion.Id);

        var response = await EnviarWebhookAsync("checkout.session.completed", sesion, secret: "whsec_otro_secreto");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task Webhook_Completed_PaysOnceEvenIfTheEventIsDeliveredTwice()
    {
        var edicion = await CrearEdicionReservableAsync();
        var (creada, sesion, reservaId) = await ReservaPendienteAsync(edicion.Id);
        var eventId = $"evt_{Guid.NewGuid():N}";

        var primera = await EnviarWebhookAsync("checkout.session.completed", sesion, eventId: eventId);
        var pagada = (await BuscarReservaAsync(creada.Codigo))!;
        var segunda = await EnviarWebhookAsync("checkout.session.completed", sesion, eventId: eventId);

        primera.StatusCode.Should().Be(HttpStatusCode.OK);
        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
        pagada.Estado.Should().Be(EstadoReserva.Pagada);
        pagada.StripePaymentIntentId.Should().Be("pi_test_123");
        pagada.PagadaEn!.Value.Should().BeCloseTo(Now, Precision);
        var trasDuplicado = (await BuscarReservaAsync(creada.Codigo))!;
        trasDuplicado.Version.Should().Be(pagada.Version, "the duplicate did not touch the booking");
        EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == reservaId).Should().Be(1);
    }

    [Fact]
    public async Task Webhook_CompletedWithAnotherEventIdForAPaidBooking_PublishesNothingNew()
    {
        var edicion = await CrearEdicionReservableAsync();
        var (_, sesion, reservaId) = await ReservaPendienteAsync(edicion.Id);

        await EnviarWebhookAsync("checkout.session.completed", sesion);
        var otra = await EnviarWebhookAsync("checkout.session.completed", sesion);

        otra.StatusCode.Should().Be(HttpStatusCode.OK);
        EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == reservaId).Should().Be(1);
    }

    [Fact]
    public async Task Webhook_Expired_ExpiresTheBookingAndFreesTheSeat()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 1);
        var (creada, sesion, reservaId) = await ReservaPendienteAsync(edicion.Id);

        var response = await EnviarWebhookAsync("checkout.session.expired", sesion, paymentStatus: "unpaid");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Expirada);
        EventRecorder.Count<ReservaExpiradaEvent>(e => e.ReservaId == reservaId).Should().Be(1);
        (await ReservarAsync(Formulario(edicion.Id))).StatusCode.Should().Be(HttpStatusCode.OK, "the seat is free again");
    }

    [Fact]
    public async Task Webhook_CompletedForExpiredBookingWithFreeSeat_PaysIt()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 2);
        var (creada, sesion, reservaId) = await ReservaPendienteAsync(edicion.Id);
        await MarcarAsync(creada.Codigo, EstadoReserva.Expirada);

        var response = await EnviarWebhookAsync("checkout.session.completed", sesion);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        reserva.Estado.Should().Be(EstadoReserva.Pagada);
        reserva.RequiereRevision.Should().BeFalse();
        EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == reservaId).Should().Be(1);
    }

    [Fact]
    public async Task Webhook_CompletedForExpiredBookingWithoutSeat_FlagsReviewAndPublishesRefundEvent()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 1);
        var (creada, sesion, reservaId) = await ReservaPendienteAsync(edicion.Id);
        await MarcarAsync(creada.Codigo, EstadoReserva.Expirada);
        (await ReservarAsync(Formulario(edicion.Id))).StatusCode.Should().Be(HttpStatusCode.OK, "someone else took the seat");

        var response = await EnviarWebhookAsync("checkout.session.completed", sesion);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        reserva.Estado.Should().Be(EstadoReserva.Expirada);
        reserva.RequiereRevision.Should().BeTrue();
        EventRecorder.Count<PagoTrasCaducidadSinPlazaEvent>(e => e.ReservaId == reservaId).Should().Be(1);
        EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == reservaId).Should().Be(0);
    }

    [Fact]
    public async Task Webhook_CompletedForCancelledBooking_UsesClientReferenceWhenSessionIsUnknown()
    {
        var edicion = await CrearEdicionReservableAsync();
        var (creada, _, reservaId) = await ReservaPendienteAsync(edicion.Id);
        await MarcarAsync(creada.Codigo, EstadoReserva.Cancelada);

        var response = await EnviarWebhookAsync("checkout.session.completed", "cs_desconocida", clientReferenceId: reservaId.ToString());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public async Task Webhook_SessionWithoutBooking_Returns200()
    {
        var response = await EnviarWebhookAsync("checkout.session.completed", "cs_no_existe");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_OtherEventType_Returns200()
    {
        var response = await EnviarWebhookAsync("payment_intent.created", "pi_x");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using FluentAssertions;
using IntegrationTests.TestSupport;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>POST /api/reservas/manual and the payment details of GET /api/reservas/{codigo}.</summary>
public class ReservaManualTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    [Fact]
    public async Task Manual_WithFreeSeats_ReturnsPaymentDetailsWhatsAppAndPublishesEvent()
    {
        var edicion = await CrearEdicionReservableAsync();

        var creada = await ReservarManualOkAsync(edicion.Id);

        (creada.Estado, creada.Metodo, creada.Importe).Should().Be(("Pendiente", "Manual", 120m));
        creada.DatosPago.Should().Be(new DatosPagoTestDto(
            CustomWebApplicationFactory.PagoTitular, CustomWebApplicationFactory.PagoIban, "600111222", creada.Codigo));
        creada.WhatsappUrl.Should().StartWith("https://wa.me/34600111222?text=");
        Uri.UnescapeDataString(creada.WhatsappUrl!).Should().Contain($"reserva {creada.Codigo} (Curso de test, ").And.EndWith("Importe: 120,00 €.");
        creada.ExpiraEn!.Value.Should().BeCloseTo(Now.AddHours(48), Precision);

        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        reserva.Metodo.Should().Be(MetodoPago.Manual);
        reserva.StripeSessionId.Should().BeNull();
        EventRecorder.Count<ReservaManualCreadaEvent>(e => e.ReservaId == reserva.Id).Should().Be(1);
    }

    [Fact]
    public async Task Obtener_PendingManual_ReturnsThePaymentDetailsAgainUntilPaid()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarManualOkAsync(edicion.Id);

        var pendiente = await ReadAsync<ReservaPublicaTestDto>(await SendAsync(HttpMethod.Get, $"/api/reservas/{creada.Codigo}"));
        await ModificarReservaAsync(creada.Codigo, r => r.ConfirmarPagoManual(Now, true, null));
        var pagada = await ReadAsync<ReservaPublicaTestDto>(await SendAsync(HttpMethod.Get, $"/api/reservas/{creada.Codigo}"));

        pendiente.DatosPago.Should().Be(creada.DatosPago);
        pendiente.WhatsappUrl.Should().Be(creada.WhatsappUrl);
        pagada.Estado.Should().Be("Pagada");
        pagada.DatosPago.Should().BeNull();
        pagada.WhatsappUrl.Should().BeNull();
        pagada.ExpiraEn.Should().BeNull();
    }

    [Fact]
    public async Task Manual_EditionClosingInLessThanTwoHours_Returns409OnlyCard()
    {
        var curso = await CrearCursoEnBdAsync();
        var edicion = await CrearEdicionEnBdAsync(curso, Now.AddHours(25)); // bookable, closes in 1 h

        var response = await ReservarManualAsync(Formulario(edicion.Id));

        await ShouldBeProblemWithCodeAsync(response, 409, "edicion_no_reservable");
        (await ReadJsonAsync(response)).GetProperty("detail").GetString().Should().Be("Para esta fecha solo se puede pagar con tarjeta");
        (await ContarReservasAsync(edicion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Manual_EditionClosingSoon_ClipsExpiryToTheClosingTime()
    {
        var curso = await CrearCursoEnBdAsync();
        var edicion = await CrearEdicionEnBdAsync(curso, Now.AddHours(30)); // closes in 6 h

        var creada = await ReservarManualOkAsync(edicion.Id);

        creada.ExpiraEn!.Value.Should().BeCloseTo(edicion.Inicio.AddHours(-24), Precision);
    }

    [Fact]
    public async Task Manual_Honeypot_Returns200AndStoresNothing()
    {
        var edicion = await CrearEdicionReservableAsync();

        var response = await ReservarManualAsync(Formulario(edicion.Id, website: "http://spam"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var falsa = await ReadAsync<ReservaPublicaTestDto>(response);
        falsa.DatosPago.Should().NotBeNull();
        (await ContarReservasAsync(edicion.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Manual_InvalidForm_Returns400()
    {
        var edicion = await CrearEdicionReservableAsync();

        var response = await ReservarManualAsync(Formulario(edicion.Id, telefono: "12", aceptaCondiciones: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

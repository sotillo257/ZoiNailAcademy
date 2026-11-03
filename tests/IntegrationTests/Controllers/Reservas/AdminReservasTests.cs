using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestSupport;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>POST /api/admin/reservas/{id}/confirmar | cancelar | ampliar and the edition cancellation cascade.</summary>
public class AdminReservasTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    private async Task<string> AdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

    private async Task<HttpResponseMessage> AccionAsync(string codigo, string accion, object body, string token)
    {
        var id = (await BuscarReservaAsync(codigo))!.Id;
        return await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{id}/{accion}", content: JsonContent.Create(body), accessToken: token);
    }

    [Fact]
    public async Task Confirmar_PendingManual_PaysAndPublishesOnceEvenIfRepeated()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);

        var primera = await AccionAsync(creada.Codigo, "confirmar", new { version = await VersionAsync(creada.Codigo), nota = "Bizum recibido" }, token);
        var accion = await ReadAsync<ReservaAccionTestDto>(primera);
        var segunda = await AccionAsync(creada.Codigo, "confirmar", new { version = accion.Reserva.Version }, token);

        primera.StatusCode.Should().Be(HttpStatusCode.OK);
        accion.Reserva.Estado.Should().Be("Pagada");
        accion.Reserva.NotaInterna.Should().Be("Bizum recibido");
        accion.Reserva.Nombre.Should().Be("Lucía Pérez");
        segunda.StatusCode.Should().Be(HttpStatusCode.OK, "confirming a paid booking is idempotent");
        EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == accion.Reserva.Id).Should().Be(1);
    }

    [Fact]
    public async Task Confirmar_StaleVersion_Returns409Concurrencia()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);
        var leida = await VersionAsync(creada.Codigo);
        await ModificarReservaAsync(creada.Codigo, r => r.Expirar(Now));

        var response = await AccionAsync(creada.Codigo, "confirmar", new { version = leida }, token);

        await ShouldBeProblemWithCodeAsync(response, 409, "concurrencia");
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Expirada);
    }

    [Fact]
    public async Task Confirmar_CardBooking_Returns409EstadoInvalido()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarOkAsync((await CrearEdicionReservableAsync()).Id);

        var response = await AccionAsync(creada.Codigo, "confirmar", new { version = await VersionAsync(creada.Codigo) }, token);

        await ShouldBeProblemWithCodeAsync(response, 409, "estado_invalido");
        (await ReadJsonAsync(response)).GetProperty("detail").GetString().Should().Be("Los pagos con tarjeta se confirman solos");
    }

    [Fact]
    public async Task Confirmar_ExpiredWithFreeSeat_Pays()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarManualOkAsync((await CrearEdicionReservableAsync(plazas: 1)).Id);
        await ModificarReservaAsync(creada.Codigo, r => r.Expirar(Now));

        var response = await AccionAsync(creada.Codigo, "confirmar", new { version = await VersionAsync(creada.Codigo) }, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public async Task Confirmar_ExpiredWithoutSeat_Returns409SinPlazas()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync(plazas: 1);
        var creada = await ReservarManualOkAsync(edicion.Id);
        await ModificarReservaAsync(creada.Codigo, r => r.Expirar(Now));
        await ReservarOkAsync(edicion.Id); // someone else takes the freed seat

        var response = await AccionAsync(creada.Codigo, "confirmar", new { version = await VersionAsync(creada.Codigo) }, token);

        await ShouldBeProblemWithCodeAsync(response, 409, "sin_plazas");
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Expirada);
    }

    [Fact]
    public async Task Cancelar_Paid_RequiresConfirmationAndWarnsToRefund()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);
        await ModificarReservaAsync(creada.Codigo, r => r.ConfirmarPagoManual(Now, true, null));

        var sinConfirmar = await AccionAsync(creada.Codigo, "cancelar", new { version = await VersionAsync(creada.Codigo) }, token);
        var confirmada = await AccionAsync(creada.Codigo, "cancelar",
            new { version = await VersionAsync(creada.Codigo), motivo = "Baja por enfermedad", confirmarPagada = true }, token);

        await ShouldBeProblemWithCodeAsync(sinConfirmar, 409, "estado_invalido");
        confirmada.StatusCode.Should().Be(HttpStatusCode.OK);
        var accion = await ReadAsync<ReservaAccionTestDto>(confirmada);
        accion.Reserva.Estado.Should().Be("Cancelada");
        accion.Avisos.Should().Equal("Recuerda reembolsar el pago");
        EventRecorder.Count<ReservaCanceladaEvent>(e => e.ReservaId == accion.Reserva.Id).Should().Be(1);
    }

    [Fact]
    public async Task Cancelar_PendingCard_ExpiresTheStripeSession()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarOkAsync((await CrearEdicionReservableAsync()).Id);
        var sesion = (await BuscarReservaAsync(creada.Codigo))!.StripeSessionId!;

        var response = await AccionAsync(creada.Codigo, "cancelar", new { version = await VersionAsync(creada.Codigo) }, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.StripeGateway.FueExpirada(sesion).Should().BeTrue();
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Ampliar_AddsHoursAndClipsToTheClosingTime()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var edicion = await CrearEdicionEnBdAsync(curso, Now.AddHours(124)); // closes in 100 h
        var creada = await ReservarManualOkAsync(edicion.Id);              // expires in 48 h

        var mas24 = await ReadAsync<ReservaAccionTestDto>(
            await AccionAsync(creada.Codigo, "ampliar", new { version = await VersionAsync(creada.Codigo), horas = 24 }, token));
        var mas168 = await ReadAsync<ReservaAccionTestDto>(
            await AccionAsync(creada.Codigo, "ampliar", new { version = mas24.Reserva.Version, horas = 168 }, token));

        mas24.Reserva.ExpiraEn.Should().BeCloseTo(Now.AddHours(72), Precision);
        mas24.Avisos.Should().BeEmpty();
        mas168.Reserva.ExpiraEn.Should().BeCloseTo(edicion.Inicio.AddHours(-24), Precision);
        mas168.Avisos.Should().ContainSingle().Which.Should().Contain("recortado");
    }

    [Fact]
    public async Task Ampliar_CardBookingOrInvalidHours_IsRejected()
    {
        var token = await AdminTokenAsync();
        var tarjeta = await ReservarOkAsync((await CrearEdicionReservableAsync()).Id);
        var manual = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);

        await ShouldBeProblemWithCodeAsync(
            await AccionAsync(tarjeta.Codigo, "ampliar", new { version = await VersionAsync(tarjeta.Codigo), horas = 24 }, token),
            409, "estado_invalido");
        (await AccionAsync(manual.Codigo, "ampliar", new { version = await VersionAsync(manual.Codigo), horas = 169 }, token))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Acciones_UnknownBooking_Return404()
    {
        var response = await SendAsync(
            HttpMethod.Post, $"/api/admin/reservas/{Guid.NewGuid()}/confirmar", content: JsonContent.Create(new { version = "1" }),
            accessToken: await AdminTokenAsync());

        await ShouldBeProblemWithCodeAsync(response, 404, "no_encontrado");
    }

    [Fact]
    public async Task CancelarEdicion_CancelsItsPendingBookingsAndWarnsAboutPaidOnes()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync();
        var pagada = await ReservarManualOkAsync(edicion.Id);
        await ModificarReservaAsync(pagada.Codigo, r => r.ConfirmarPagoManual(Now, true, null));
        var manual = await ReservarManualOkAsync(edicion.Id);
        var tarjeta = await ReservarOkAsync(edicion.Id);
        var sesion = (await BuscarReservaAsync(tarjeta.Codigo))!.StripeSessionId!;
        var dto = await ReadAsync<Catalogo.EdicionAdminTestDto>(
            await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{edicion.Id}", accessToken: token));

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/ediciones/{edicion.Id}",
            content: JsonContent.Create(new { inicio = dto.Inicio, plazas = dto.Plazas, estado = "Cancelada", version = dto.Version }),
            accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadAsync<Catalogo.EdicionEditadaTestDto>(response)).Avisos
            .Should().Equal("Tiene 1 inscritos: avisa y reembolsa manualmente");
        (await BuscarReservaAsync(pagada.Codigo))!.Estado.Should().Be(EstadoReserva.Pagada);
        (await BuscarReservaAsync(manual.Codigo))!.Estado.Should().Be(EstadoReserva.Cancelada);
        (await BuscarReservaAsync(tarjeta.Codigo))!.Estado.Should().Be(EstadoReserva.Cancelada);
        Factory.StripeGateway.FueExpirada(sesion).Should().BeTrue();
        var manualId = (await BuscarReservaAsync(manual.Codigo))!.Id;
        EventRecorder.Count<ReservaCanceladaEvent>(e => e.ReservaId == manualId).Should().Be(1);
    }
}

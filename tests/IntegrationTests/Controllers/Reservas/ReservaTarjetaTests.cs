using System.Net;
using FluentAssertions;
using IntegrationTests.TestSupport;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>POST /api/reservas/tarjeta, GET /api/reservas/{codigo} and POST /api/reservas/{codigo}/abandonar.</summary>
public class ReservaTarjetaTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    [Fact]
    public async Task Tarjeta_WithFreeSeats_CreatesPendingBookingWithCheckoutSession()
    {
        var edicion = await CrearEdicionReservableAsync();

        var response = await ReservarAsync(Formulario(edicion.Id, email: " Lucia.Test@Mail.TEST ", telefono: "612 99 88 77"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var creada = await ReadAsync<ReservaTarjetaTestDto>(response);
        creada.Codigo.Should().MatchRegex("^CUR-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{6}$");
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        reserva.Metodo.Should().Be(MetodoPago.Stripe);
        reserva.Email.Should().Be("lucia.test@mail.test");
        reserva.Telefono.Should().Be("+34612998877");
        reserva.Importe.Should().Be(120m);
        reserva.ExpiraEn.Should().BeCloseTo(Now.AddMinutes(35), Precision);
        reserva.StripeSessionId.Should().NotBeNull();
        creada.CheckoutUrl.Should().EndWith(reserva.StripeSessionId);

        var sesion = Factory.StripeGateway.Sesion(reserva.StripeSessionId!);
        sesion.ImporteCentimos.Should().Be(12000);
        sesion.ExpiraEn.Should().Be(Now.AddMinutes(30));
        sesion.SuccessUrl.Should().Be($"https://web.test/reserva/{creada.Codigo}?origen=stripe");
    }

    [Fact]
    public async Task Tarjeta_CourseWithSeatDeposit_ChargesOnlyTheDeposit()
    {
        var edicion = await CrearEdicionReservableAsync(precioReserva: 35.50m);

        var creada = await ReadAsync<ReservaTarjetaTestDto>(await ReservarAsync(Formulario(edicion.Id)));

        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        (reserva.Importe, reserva.PrecioCurso).Should().Be((35.50m, 120m));
        var sesion = Factory.StripeGateway.Sesion(reserva.StripeSessionId!);
        sesion.ImporteCentimos.Should().Be(3550);
        sesion.Concepto.Should().StartWith("Reserva de plaza: Curso de test — ");
    }

    [Fact]
    public async Task Tarjeta_ConcurrentRequestsForTheLastSeat_CreateExactlyOneBooking()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => ReservarAsync(Formulario(edicion.Id))));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        var rechazadas = responses.Where(r => r.StatusCode != HttpStatusCode.OK).ToList();
        rechazadas.Should().HaveCount(9);
        foreach (var rechazada in rechazadas)
        {
            await ShouldBeProblemWithCodeAsync(rechazada, 409, "sin_plazas");
        }

        (await ContarReservasAsync(edicion.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Tarjeta_EditionNotBookable_Returns409()
    {
        var curso = await CrearCursoEnBdAsync();
        var oculta = await CrearEdicionEnBdAsync(curso, Now.AddDays(20), estado: EstadoEdicion.Oculta);
        var dentroDelCierre = await CrearEdicionEnBdAsync(curso, Now.AddHours(5));

        await ShouldBeProblemWithCodeAsync(await ReservarAsync(Formulario(oculta.Id)), 409, "edicion_no_reservable");
        await ShouldBeProblemWithCodeAsync(await ReservarAsync(Formulario(dentroDelCierre.Id)), 409, "edicion_no_reservable");
    }

    [Fact]
    public async Task Tarjeta_UnknownEdition_Returns404()
    {
        await ShouldBeProblemWithCodeAsync(await ReservarAsync(Formulario(Guid.NewGuid())), 404, "no_encontrado");
    }

    [Fact]
    public async Task Tarjeta_ThirdPendingWithSamePhone_Returns429LimitePendientes()
    {
        var edicion = await CrearEdicionReservableAsync();
        var telefono = UniqueTelefono();

        (await ReservarAsync(Formulario(edicion.Id, telefono: telefono))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReservarAsync(Formulario(edicion.Id, telefono: telefono))).StatusCode.Should().Be(HttpStatusCode.OK);
        var tercera = await ReservarAsync(Formulario(edicion.Id, telefono: telefono));

        await ShouldBeProblemWithCodeAsync(tercera, 429, "limite_pendientes");
    }

    [Fact]
    public async Task Tarjeta_Honeypot_Returns200AndCreatesNothing()
    {
        var edicion = await CrearEdicionReservableAsync();

        var response = await ReservarAsync(Formulario(edicion.Id, website: "http://spam.example"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var falsa = await ReadAsync<ReservaTarjetaTestDto>(response);
        falsa.Codigo.Should().StartWith("CUR-");
        (await ContarReservasAsync(edicion.Id)).Should().Be(0);
        (await BuscarReservaAsync(falsa.Codigo)).Should().BeNull();
    }

    [Fact]
    public async Task Tarjeta_InvalidForm_Returns400PerField()
    {
        var edicion = await CrearEdicionReservableAsync();

        var response = await ReservarAsync(Formulario(edicion.Id, email: "no-es-email", telefono: "123", aceptaCondiciones: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        foreach (var campo in new[] { "Email", "Telefono", "AceptaCondiciones" })
        {
            errors.TryGetProperty(campo, out _).Should().BeTrue(campo);
        }
    }

    [Fact]
    public async Task Tarjeta_StripeFails_Returns502AndCancelsTheBooking()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 1);

        var response = await ReservarAsync(Formulario(edicion.Id, email: $"{FakeStripeGateway.EmailQueFalla}@mail.test"));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await ReadJsonAsync(response)).GetProperty("detail").GetString().Should().Be("No se pudo iniciar el pago, inténtalo de nuevo");
        (await ReservarAsync(Formulario(edicion.Id))).StatusCode.Should().Be(HttpStatusCode.OK, "the cancelled booking freed the seat");
    }

    [Fact]
    public async Task Obtener_ReturnsPublicStateWithoutPersonalData()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarOkAsync(edicion.Id);

        var response = await SendAsync(HttpMethod.Get, $"/api/reservas/{creada.Codigo}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("Lucía").And.NotContain("@mail.test").And.NotContain("+346");
        var dto = await ReadAsync<ReservaPublicaTestDto>(response);
        (dto.Codigo, dto.Estado, dto.Metodo, dto.Curso, dto.Importe).Should().Be((creada.Codigo, "Pendiente", "Stripe", "Curso de test", 120m));
        dto.Inicio.Should().BeCloseTo(edicion.Inicio, Precision);
        dto.ExpiraEn!.Value.Should().BeCloseTo(Now.AddMinutes(35), Precision);
        dto.DatosPago.Should().BeNull();
    }

    [Fact]
    public async Task Obtener_UnknownCode_Returns404()
    {
        await ShouldBeProblemWithCodeAsync(await SendAsync(HttpMethod.Get, "/api/reservas/CUR-ZZZZZZ"), 404, "no_encontrado");
    }

    [Fact]
    public async Task Abandonar_Pending_CancelsAndExpiresTheSession()
    {
        var edicion = await CrearEdicionReservableAsync(plazas: 1);
        var creada = await ReservarOkAsync(edicion.Id);

        var response = await SendAsync(HttpMethod.Post, $"/api/reservas/{creada.Codigo}/abandonar");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        Factory.StripeGateway.FueExpirada(reserva.StripeSessionId!).Should().BeTrue();
        (await ReservarAsync(Formulario(edicion.Id))).StatusCode.Should().Be(HttpStatusCode.OK, "the seat is free again");
    }

    [Fact]
    public async Task Abandonar_Paid_ChangesNothing()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarOkAsync(edicion.Id);
        var sesion = (await BuscarReservaAsync(creada.Codigo))!.StripeSessionId!;
        (await EnviarWebhookAsync("checkout.session.completed", sesion)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SendAsync(HttpMethod.Post, $"/api/reservas/{creada.Codigo}/abandonar");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public async Task Abandonar_SessionAlreadyCompletedInStripe_LeavesItPending()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarOkAsync(edicion.Id);
        Factory.StripeGateway.MarcarCompletada((await BuscarReservaAsync(creada.Codigo))!.StripeSessionId!);

        var response = await SendAsync(HttpMethod.Post, $"/api/reservas/{creada.Codigo}/abandonar");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pendiente);
    }
}

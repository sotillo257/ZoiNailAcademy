using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestSupport;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.ExpirarReservasCaducadas;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Reservas;

/// <summary>
/// Expiry by time and the expiry job. Each test runs on its own host and clock (same database), because it
/// moves time forward.
/// </summary>
public class CaducidadReservasTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    private static async Task<int> EjecutarJobAsync(WebApplicationFactory<Program> host)
    {
        using var scope = host.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ExpirarReservasCaducadasCommand())).Value;
    }

    private (WebApplicationFactory<Program> Host, FakeTimeProvider Clock, HttpClient Client) NuevoHost(bool conJob = false)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var host = Factory.CreateHost(clock, conJob);
        return (host, clock, host.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false }));
    }

    [Fact]
    public async Task ExpiredManualBooking_FreesItsSeatBeforeTheJobRuns()
    {
        var (host, clock, client) = NuevoHost();
        await using var _ = host;
        var curso = await CrearCursoEnBdAsync();
        var edicion = await CrearEdicionEnBdAsync(curso, Now.AddDays(20), plazas: 1);
        var creada = await ReservarManualOkAsync(edicion.Id, client);
        var antes = await ReadAsync<List<Catalogo.EdicionPublicaTestDto>>(
            await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}/ediciones", client: client));

        clock.Advance(TimeSpan.FromHours(49));
        var despues = await ReadAsync<List<Catalogo.EdicionPublicaTestDto>>(
            await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}/ediciones", client: client));

        antes.Single().PlazasLibres.Should().Be(0);
        despues.Single().PlazasLibres.Should().Be(1);
        (await BuscarReservaAsync(creada.Codigo))!.Estado.Should().Be(EstadoReserva.Pendiente, "the job has not run");
    }

    [Fact]
    public async Task Job_ExpiresDueBookingsAndPublishesOneEventEach()
    {
        var (host, clock, client) = NuevoHost();
        await using var _ = host;
        var edicion = await CrearEdicionReservableAsync();
        var primera = await ReservarManualOkAsync(edicion.Id, client);
        var segunda = await ReservarManualOkAsync(edicion.Id, client);

        clock.Advance(TimeSpan.FromHours(49));
        await EjecutarJobAsync(host);
        await EjecutarJobAsync(host);

        foreach (var codigo in new[] { primera.Codigo, segunda.Codigo })
        {
            var reserva = (await BuscarReservaAsync(codigo))!;
            reserva.Estado.Should().Be(EstadoReserva.Expirada);
            reserva.ExpiradaEn.Should().NotBeNull();
            EventRecorder.Count<ReservaExpiradaEvent>(e => e.ReservaId == reserva.Id).Should().Be(1);
        }
    }

    [Fact]
    public async Task Job_TwoReplicasInParallel_ExpireEachBookingOnce()
    {
        var (host, clock, client) = NuevoHost();
        await using var _ = host;
        var edicion = await CrearEdicionReservableAsync(plazas: 20);
        var codigos = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            codigos.Add((await ReservarManualOkAsync(edicion.Id, client)).Codigo);
        }

        clock.Advance(TimeSpan.FromHours(49));
        var resultados = await Task.WhenAll(EjecutarJobAsync(host), EjecutarJobAsync(host), EjecutarJobAsync(host));

        resultados.Sum().Should().BeGreaterThanOrEqualTo(codigos.Count);
        foreach (var codigo in codigos)
        {
            var reserva = (await BuscarReservaAsync(codigo))!;
            reserva.Estado.Should().Be(EstadoReserva.Expirada);
            EventRecorder.Count<ReservaExpiradaEvent>(e => e.ReservaId == reserva.Id).Should().Be(1);
        }
    }

    [Fact]
    public async Task JobAndConfirmationRace_ExactlyOneWins()
    {
        var (host, clock, client) = NuevoHost();
        await using var _ = host;
        var token = (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken; // the JWT is checked with the real clock
        var edicion = await CrearEdicionReservableAsync(plazas: 20);
        var codigos = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            codigos.Add((await ReservarManualOkAsync(edicion.Id, client)).Codigo);
        }

        clock.Advance(TimeSpan.FromHours(49));
        var confirmaciones = new List<Task<HttpResponseMessage>>();
        foreach (var codigo in codigos)
        {
            var reserva = (await BuscarReservaAsync(codigo))!;
            confirmaciones.Add(SendAsync(
                HttpMethod.Post, $"/api/admin/reservas/{reserva.Id}/confirmar",
                content: JsonContent.Create(new { version = reserva.Version.ToString() }), accessToken: token, client: client));
        }

        await Task.WhenAll(EjecutarJobAsync(host), Task.WhenAll(confirmaciones));

        for (var i = 0; i < codigos.Count; i++)
        {
            var reserva = (await BuscarReservaAsync(codigos[i]))!;
            var respuesta = confirmaciones[i].Result;
            var pagadas = EventRecorder.Count<ReservaPagadaEvent>(e => e.ReservaId == reserva.Id);
            var expiradas = EventRecorder.Count<ReservaExpiradaEvent>(e => e.ReservaId == reserva.Id);

            if (reserva.Estado == EstadoReserva.Pagada)
            {
                respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
                (pagadas, expiradas).Should().Be((1, 0));
            }
            else
            {
                reserva.Estado.Should().Be(EstadoReserva.Expirada);
                await ShouldBeProblemWithCodeAsync(respuesta, 409, "concurrencia");
                (pagadas, expiradas).Should().Be((0, 1));
            }
        }
    }

    [Fact]
    public async Task BackgroundJob_RunsWhenTheIntervalElapses()
    {
        var (host, clock, client) = NuevoHost(conJob: true);
        await using var _ = host;
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarManualOkAsync(edicion.Id, client);

        clock.Advance(TimeSpan.FromHours(49)); // also ticks the job's PeriodicTimer

        var estado = EstadoReserva.Pendiente;
        for (var intento = 0; intento < 50 && estado == EstadoReserva.Pendiente; intento++)
        {
            await Task.Delay(100);
            estado = (await BuscarReservaAsync(creada.Codigo))!.Estado;
        }

        estado.Should().Be(EstadoReserva.Expirada);
    }
}

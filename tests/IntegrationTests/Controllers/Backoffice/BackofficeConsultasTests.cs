using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using IntegrationTests.Controllers.Catalogo;
using IntegrationTests.Controllers.Reservas;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.Backoffice;

/// <summary>
/// F07 queries: summary, bookings of an edition and attendee CSV, pending bookings and search, detail and
/// "reviewed" mark. The database is shared, so the summary counters are checked as differences.
/// </summary>
public class BackofficeConsultasTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private async Task<string> AdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

    private async Task<T> GetOkAsync<T>(string url, string token, HttpClient? client = null)
    {
        var response = await SendAsync(HttpMethod.Get, url, accessToken: token, client: client);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadAsync<T>(response);
    }

    private Task<ResumenTestDto> ResumenAsync(string token, HttpClient? client = null) =>
        GetOkAsync<ResumenTestDto>("/api/admin/resumen", token, client);

    private Task<List<ReservaAdminTestDto>> PendientesAsync(string token, string query = "") =>
        GetOkAsync<List<ReservaAdminTestDto>>($"/api/admin/reservas/pendientes?{query}", token);

    private async Task<Guid> IdAsync(string codigo) => (await BuscarReservaAsync(codigo))!.Id;

    /// <summary>A manual pending booking with an exact expiry, created straight in the database.</summary>
    private async Task CrearManualPendienteAsync(Guid edicionId, DateTimeOffset expiraEn)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var edicion = await db.Ediciones.Include(e => e.Curso).SingleAsync(e => e.Id == edicionId);
        db.Reservas.Add(Reserva.Create(
            edicion, CodigoReserva.Generar(), MetodoPago.Manual, "Reserva de test", $"{Guid.NewGuid():N}@mail.test",
            UniqueTelefono(), expiraEn, expiraEn.AddHours(-2)));
        await db.SaveChangesAsync();
    }

    private static DateTimeOffset HoraMadrid(DateTime fecha, int hora, int minuto)
    {
        var local = fecha.Date.AddHours(hora).AddMinutes(minuto);
        return new DateTimeOffset(local, Madrid.GetUtcOffset(local)).ToUniversalTime();
    }

    // ----- Summary -----

    [Fact]
    public async Task Resumen_SeededData_EachCounterCountsItsRows()
    {
        var token = await AdminTokenAsync();
        var antes = await ResumenAsync(token);
        var edicion = await CrearEdicionReservableAsync();
        await ReservarManualOkAsync(edicion.Id);                          // manual pending: counts
        await ReservarOkAsync(edicion.Id);                                // card pending: does not
        var pagada = await ReservarManualOkAsync(edicion.Id);              // paid: does not
        await ModificarReservaAsync(pagada.Codigo, r => r.ConfirmarPagoManual(Now, true, null));
        var revision = await ReservarOkAsync(edicion.Id);                  // needs review
        await ModificarReservaAsync(revision.Codigo, r => { r.Expirar(Now); r.RegistrarPagoSinPlaza(); });
        var curso = await CrearCursoEnBdAsync();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cursoBd = await db.Cursos.FindAsync(curso.Id);
            var contactada = ZoiNailAcademy.Domain.Entities.ListaEspera.Create(
                cursoBd!, null, null, "Eva", $"{Guid.NewGuid():N}@mail.test", UniqueTelefono(), "V1", Now);
            contactada.MarcarContactado(true, Now);
            db.ListaEspera.AddRange(
                ZoiNailAcademy.Domain.Entities.ListaEspera.Create(
                    cursoBd!, null, null, "Ana", $"{Guid.NewGuid():N}@mail.test", UniqueTelefono(), "V1", Now),
                contactada);
            await db.SaveChangesAsync();
        }

        var despues = await ResumenAsync(token);

        (despues.PendientesDePago - antes.PendientesDePago).Should().Be(1);
        (despues.RequierenRevision - antes.RequierenRevision).Should().Be(1);
        (despues.ListaEsperaSinContactar - antes.ListaEsperaSinContactar).Should().Be(1);
    }

    [Fact]
    public async Task Resumen_AroundMadridMidnight_CountsOnlyWhatExpiresBeforeMidnight()
    {
        var token = await AdminTokenAsync();
        var dia = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Madrid).Date.AddDays(2);
        var clock = new FakeTimeProvider(HoraMadrid(dia, 23, 30));
        await using var host = Factory.CreateHost(clock);
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var edicion = await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), HoraMadrid(dia.AddDays(10), 10, 0));

        var antesNoche = await ResumenAsync(token, client);
        await CrearManualPendienteAsync(edicion.Id, HoraMadrid(dia, 23, 45));
        await CrearManualPendienteAsync(edicion.Id, HoraMadrid(dia.AddDays(1), 0, 15));
        var noche = await ResumenAsync(token, client);

        clock.SetUtcNow(HoraMadrid(dia.AddDays(1), 0, 30));
        var antesMadrugada = await ResumenAsync(token, client);
        await CrearManualPendienteAsync(edicion.Id, HoraMadrid(dia.AddDays(1), 23, 50));
        await CrearManualPendienteAsync(edicion.Id, HoraMadrid(dia.AddDays(2), 0, 10));
        var madrugada = await ResumenAsync(token, client);

        (noche.PendientesDePago - antesNoche.PendientesDePago).Should().Be(2);
        (noche.PendientesQueCaducanHoy - antesNoche.PendientesQueCaducanHoy).Should().Be(1, "only 23:45 is before midnight");
        (madrugada.PendientesDePago - antesMadrugada.PendientesDePago).Should().Be(2);
        (madrugada.PendientesQueCaducanHoy - antesMadrugada.PendientesQueCaducanHoy).Should().Be(1, "only 23:50 is today");
    }

    [Fact]
    public async Task Resumen_ProximasEdiciones_AreTheNextFiveNotCancelledSoonestFirst()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        await CrearEdicionEnBdAsync(curso, Now.AddMinutes(5), estado: EstadoEdicion.Cancelada);
        var propias = new List<Edicion>();
        for (var i = 6; i >= 1; i--)
        {
            propias.Add(await CrearEdicionEnBdAsync(curso, Now.AddMinutes(10 * i), estado: i == 2 ? EstadoEdicion.Oculta : EstadoEdicion.Programada));
        }

        var resumen = await ResumenAsync(token);

        resumen.ProximasEdiciones.Select(e => e.Id).Should()
            .Equal(propias.OrderBy(e => e.Inicio).Take(5).Select(e => e.Id));
    }

    // ----- Bookings of an edition and CSV -----

    [Fact]
    public async Task ReservasEdicion_ByDefaultPaidAndPendingAndWithIncluirTheOthers()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync();
        await CrearReservasEnBdAsync(edicion.Id, pagadas: 2, pendientes: 1, expiradas: 1, canceladas: 1);

        var porDefecto = await GetOkAsync<ReservasEdicionTestDto>($"/api/admin/ediciones/{edicion.Id}/reservas", token);
        var otras = await GetOkAsync<ReservasEdicionTestDto>(
            $"/api/admin/ediciones/{edicion.Id}/reservas?incluir=expiradas,canceladas", token);
        var invalido = await SendAsync(
            HttpMethod.Get, $"/api/admin/ediciones/{edicion.Id}/reservas?incluir=todas", accessToken: token);
        var desconocida = await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{Guid.NewGuid()}/reservas", accessToken: token);

        porDefecto.Edicion.Id.Should().Be(edicion.Id);
        porDefecto.Edicion.Pagadas.Should().Be(2);
        porDefecto.Reservas.Select(r => r.Estado).Should().Equal("Pagada", "Pagada", "Pendiente");
        porDefecto.Reservas.Should().OnlyContain(r => r.EdicionId == edicion.Id && r.Version != "");
        otras.Reservas.Select(r => r.Estado).Should().Equal("Expirada", "Cancelada");
        invalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeProblemWithCodeAsync(desconocida, 404, "no_encontrado");
    }

    [Fact]
    public async Task ReservasCsv_OnlyPaidWithBomAndSemicolons()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var edicion = await CrearEdicionEnBdAsync(curso, Now.AddDays(20));
        var pagada = await ReservarManualOkAsync(edicion.Id);
        await ModificarReservaAsync(pagada.Codigo, r => r.ConfirmarPagoManual(Now, true, null));
        var pendiente = await ReservarManualOkAsync(edicion.Id);

        var response = await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{edicion.Id}/reservas.csv", accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var inicio = TimeZoneInfo.ConvertTime(edicion.Inicio, Madrid).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        response.Content.Headers.ContentDisposition!.FileName.Should().Contain($"asistentes-{curso.Slug}-{inicio}.csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var lineas = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lineas.Should().HaveCount(2);
        lineas[0].Should().Be("Código;Nombre;Teléfono;Email;Método;Importe;Resto en persona;Fecha de pago");
        lineas[1].Should().StartWith($"{pagada.Codigo};Lucía Pérez;'+34").And.Contain(";Transferencia o Bizum;120,00;0,00;");
        string.Concat(lineas).Should().NotContain(pendiente.Codigo);
        (await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{Guid.NewGuid()}/reservas.csv", accessToken: token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- Pending bookings and search -----

    [Theory]
    [InlineData("{0}")]
    [InlineData("cur {1}")]
    [InlineData("{2}")]
    public async Task Pendientes_CodeTypedInDifferentWays_FindsTheSameBooking(string formato)
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync();
        var buscada = await ReservarManualOkAsync(edicion.Id);
        var otra = await ReservarManualOkAsync(edicion.Id);
        var aleatorio = buscada.Codigo[CodigoReserva.Prefijo.Length..];
        var q = string.Format(formato, buscada.Codigo, aleatorio.ToLowerInvariant(), aleatorio);

        var result = await PendientesAsync(token, $"q={Uri.EscapeDataString(q)}");

        result.Select(r => r.Codigo).Should().Contain(buscada.Codigo).And.NotContain(otra.Codigo);
        result.Single(r => r.Codigo == buscada.Codigo).Metodo.Should().Be("Manual");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pendientes_PhoneWithoutPrefixOrWithSpaces_FindsTheBooking(bool conEspacios)
    {
        var token = await AdminTokenAsync();
        var telefono = UniqueTelefono();                                   // +346XXXXXXXX
        var response = await ReservarManualAsync(Formulario((await CrearEdicionReservableAsync()).Id, telefono: telefono));
        var creada = await ReadAsync<ReservaPublicaTestDto>(response);
        var nacional = telefono[3..];                                       // 6XXXXXXXX
        var q = conEspacios ? $"{nacional[..3]} {nacional[3..5]} {nacional[5..7]} {nacional[7..]}" : nacional;

        var result = await PendientesAsync(token, $"q={Uri.EscapeDataString(q)}");

        result.Should().ContainSingle().Which.Codigo.Should().Be(creada.Codigo);
    }

    [Fact]
    public async Task Pendientes_ManualExpired3DaysAgoIsListedButNot10DaysAgoNorCard()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync();
        var hace3 = await ReservarManualOkAsync(edicion.Id);
        var hace10 = await ReservarManualOkAsync(edicion.Id);
        var tarjeta = await ReservarOkAsync(edicion.Id);
        await ModificarReservaAsync(hace3.Codigo, r => r.Expirar(Now.AddDays(-3)));
        await ModificarReservaAsync(hace10.Codigo, r => r.Expirar(Now.AddDays(-10)));
        await ModificarReservaAsync(tarjeta.Codigo, r => r.Expirar(Now.AddDays(-1)));

        var codigos = (await PendientesAsync(token)).Select(r => r.Codigo).ToList();

        codigos.Should().Contain(hace3.Codigo).And.NotContain(hace10.Codigo).And.NotContain(tarjeta.Codigo);
    }

    [Fact]
    public async Task Pendientes_PlazaDisponible_TellsWhetherAnExpiredBookingCouldStillBeConfirmed()
    {
        var token = await AdminTokenAsync();
        var conPlaza = await ReservarManualOkAsync((await CrearEdicionReservableAsync(plazas: 1)).Id);
        await ModificarReservaAsync(conPlaza.Codigo, r => r.Expirar(Now));
        var llena = await CrearEdicionReservableAsync(plazas: 1);
        var sinPlaza = await ReservarManualOkAsync(llena.Id);
        await ModificarReservaAsync(sinPlaza.Codigo, r => r.Expirar(Now));
        await ReservarOkAsync(llena.Id);                                    // someone else takes the seat
        var pendiente = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);

        var result = await PendientesAsync(token, "metodo=Manual");
        var json = await ReadJsonAsync(await SendAsync(
            HttpMethod.Get, $"/api/admin/reservas/pendientes?q={pendiente.Codigo}", accessToken: token));

        result.Single(r => r.Codigo == conPlaza.Codigo).PlazaDisponible.Should().BeTrue();
        result.Single(r => r.Codigo == sinPlaza.Codigo).PlazaDisponible.Should().BeFalse();
        result.Single(r => r.Codigo == pendiente.Codigo).PlazaDisponible.Should().BeNull();
        json[0].TryGetProperty("plazaDisponible", out _).Should().BeFalse("it is omitted for pending bookings");
        result.Should().OnlyContain(r => r.Metodo == "Manual" || r.RequiereRevision);
    }

    [Fact]
    public async Task Pendientes_NeedsReviewFirstThenPendingSoonestToExpire()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionReservableAsync();
        var tarjeta = await ReservarOkAsync(edicion.Id);                     // expires in 30 min
        var manual = await ReservarManualOkAsync(edicion.Id);                // expires in 48 h
        var revision = await ReservarOkAsync(edicion.Id);
        await ModificarReservaAsync(revision.Codigo, r => { r.Expirar(Now); r.RegistrarPagoSinPlaza(); });

        var codigos = (await PendientesAsync(token)).Select(r => r.Codigo).ToList();

        codigos.IndexOf(revision.Codigo).Should().BeLessThan(codigos.IndexOf(tarjeta.Codigo));
        codigos.IndexOf(tarjeta.Codigo).Should().BeLessThan(codigos.IndexOf(manual.Codigo));
    }

    // ----- Detail and reviewed mark -----

    [Fact]
    public async Task Detalle_AnyState_ReturnsTheBookingAndUnknownIs404()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarManualOkAsync((await CrearEdicionReservableAsync()).Id);
        await ModificarReservaAsync(creada.Codigo, r => r.Cancelar(Now));

        var detalle = await GetOkAsync<ReservaAdminTestDto>($"/api/admin/reservas/{await IdAsync(creada.Codigo)}", token);
        var desconocida = await SendAsync(HttpMethod.Get, $"/api/admin/reservas/{Guid.NewGuid()}", accessToken: token);

        detalle.Codigo.Should().Be(creada.Codigo);
        detalle.Estado.Should().Be("Cancelada");
        detalle.Nombre.Should().Be("Lucía Pérez");
        detalle.Version.Should().Be(await VersionAsync(creada.Codigo));
        await ShouldBeProblemWithCodeAsync(desconocida, 404, "no_encontrado");
    }

    [Fact]
    public async Task MarcarRevisada_ClearsTheMarkStoresTheNoteAndLeavesTheSummary()
    {
        var token = await AdminTokenAsync();
        var creada = await ReservarOkAsync((await CrearEdicionReservableAsync()).Id);
        await ModificarReservaAsync(creada.Codigo, r => { r.Expirar(Now); r.RegistrarPagoSinPlaza("pi_tarde"); });
        var id = await IdAsync(creada.Codigo);
        var leida = await VersionAsync(creada.Codigo);
        var antes = await ResumenAsync(token);

        var response = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{id}/marcar-revisada",
            content: JsonContent.Create(new { version = leida, nota = "Reembolsado el 05/12" }), accessToken: token);
        var repetida = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{id}/marcar-revisada",
            content: JsonContent.Create(new { version = leida }), accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var accion = await ReadAsync<ReservaAccionTestDto>(response);
        accion.Reserva.RequiereRevision.Should().BeFalse();
        accion.Reserva.NotaInterna.Should().Be("Reembolsado el 05/12");
        accion.Avisos.Should().BeEmpty();
        (await BuscarReservaAsync(creada.Codigo))!.RequiereRevision.Should().BeFalse();
        (antes.RequierenRevision - (await ResumenAsync(token)).RequierenRevision).Should().Be(1);
        await ShouldBeProblemWithCodeAsync(repetida, 409, "concurrencia");
    }

    // Without a token every /api/admin endpoint answers 401: AuthorizationTests discovers them dynamically.
}

public record ResumenTestDto(
    int PendientesDePago, int PendientesQueCaducanHoy, int RequierenRevision, int ListaEsperaSinContactar,
    List<EdicionAdminTestDto> ProximasEdiciones);

public record ReservasEdicionTestDto(EdicionAdminTestDto Edicion, List<ReservaAdminTestDto> Reservas);

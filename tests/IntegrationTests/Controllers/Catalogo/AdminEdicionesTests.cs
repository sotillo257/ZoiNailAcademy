using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestSupport;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Catalogo;

/// <summary>Edition CRUD of the backoffice (/api/admin/ediciones).</summary>
public class AdminEdicionesTests(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    private async Task<string> AdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

    private async Task<EdicionAdminTestDto> GetAsync(Guid id, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{id}", accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadAsync<EdicionAdminTestDto>(response);
    }

    private Task<HttpResponseMessage> PutAsync(Guid id, object body, string token) =>
        SendAsync(HttpMethod.Put, $"/api/admin/ediciones/{id}", content: JsonContent.Create(body), accessToken: token);

    private static object Body(EdicionAdminTestDto e, DateTimeOffset? inicio = null, int? plazas = null,
        string? estado = null, string? notas = null, string? version = null, bool? confirmarCambioFecha = null) => new
    {
        inicio = inicio ?? e.Inicio,
        plazas = plazas ?? e.Plazas,
        estado = estado ?? e.Estado,
        notas = notas ?? e.Notas,
        version = version ?? e.Version,
        confirmarCambioFecha = confirmarCambioFecha ?? false
    };

    [Fact]
    public async Task Crear_WithoutState_Returns201ScheduledAndPublishesEvent()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/ediciones",
            content: JsonContent.Create(new { cursoId = curso.Id, inicio = Now.AddDays(15), plazas = 12, notas = "Sala 2" }),
            accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var edicion = await ReadAsync<EdicionAdminTestDto>(response);
        edicion.Estado.Should().Be("Programada");
        (edicion.Plazas, edicion.Pagadas, edicion.Pendientes, edicion.Libres).Should().Be((12, 0, 0, 12));
        edicion.Curso.Should().Be(curso.Nombre);
        edicion.Reservable.Should().BeTrue();
        edicion.Notas.Should().Be("Sala 2");
        uint.Parse(edicion.Version).Should().BeGreaterThan(0);
        response.Headers.Location!.ToString().Should().EndWith($"/api/admin/ediciones/{edicion.Id}");
        EventRecorder.Count<EdicionPublicadaEvent>(e => e.EdicionId == edicion.Id).Should().Be(1);
    }

    [Fact]
    public async Task Crear_Hidden_DoesNotPublishEvent()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/ediciones",
            content: JsonContent.Create(new { cursoId = curso.Id, inicio = Now.AddDays(15), plazas = 12, estado = "Oculta" }),
            accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var edicion = await ReadAsync<EdicionAdminTestDto>(response);
        edicion.Reservable.Should().BeFalse();
        EventRecorder.Count<EdicionPublicadaEvent>(e => e.EdicionId == edicion.Id).Should().Be(0);
    }

    [Fact]
    public async Task Crear_InvalidData_Returns400PerField()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/ediciones",
            content: JsonContent.Create(new { cursoId = curso.Id, inicio = Now.AddDays(-1), plazas = 0, estado = "Cancelada" }),
            accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        errors.TryGetProperty("Inicio", out _).Should().BeTrue();
        errors.TryGetProperty("Plazas", out _).Should().BeTrue();
        errors.TryGetProperty("Estado", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Crear_UnknownCourse_Returns404()
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/ediciones",
            content: JsonContent.Create(new { cursoId = Guid.NewGuid(), inicio = Now.AddDays(15), plazas = 12 }),
            accessToken: await AdminTokenAsync());

        await ShouldBeProblemWithCodeAsync(response, 404, "no_encontrado");
    }

    [Fact]
    public async Task Listar_FiltersByCourseAndIncludesPastOnlyWhenAsked()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lejana = await CrearEdicionEnBdAsync(curso, Now.AddDays(50));
        var proxima = await CrearEdicionEnBdAsync(curso, Now.AddDays(5));
        var pasada = await CrearEdicionEnBdAsync(curso, Now.AddDays(-10));
        var otroCurso = await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(6));
        await CrearReservasEnBdAsync(proxima.Id, pagadas: 3, pendientes: 2);

        var sinPasadas = await ReadAsync<List<EdicionAdminTestDto>>(
            await SendAsync(HttpMethod.Get, $"/api/admin/ediciones?cursoId={curso.Id}", accessToken: token));
        var conPasadas = await ReadAsync<List<EdicionAdminTestDto>>(
            await SendAsync(HttpMethod.Get, $"/api/admin/ediciones?cursoId={curso.Id}&incluirPasadas=true", accessToken: token));

        sinPasadas.Select(e => e.Id).Should().Equal(proxima.Id, lejana.Id);
        conPasadas.Select(e => e.Id).Should().Equal(proxima.Id, lejana.Id, pasada.Id);
        conPasadas.Should().NotContain(e => e.Id == otroCurso.Id);
        var dto = sinPasadas[0];
        (dto.Pagadas, dto.Pendientes, dto.Libres).Should().Be((3, 2, 5));
    }

    [Fact]
    public async Task Obtener_Unknown_Returns404NoEncontrado()
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{Guid.NewGuid()}", accessToken: await AdminTokenAsync());

        await ShouldBeProblemWithCodeAsync(response, 404, "no_encontrado");
    }

    [Fact]
    public async Task Editar_ValidChange_Returns200WithNewVersion()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);

        var response = await PutAsync(edicion.Id, Body(edicion, plazas: 15, notas: "Ampliada"), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var editada = await ReadAsync<EdicionEditadaTestDto>(response);
        editada.Edicion.Plazas.Should().Be(15);
        editada.Edicion.Notas.Should().Be("Ampliada");
        editada.Edicion.Version.Should().NotBe(edicion.Version);
        editada.Avisos.Should().BeEmpty();
    }

    [Fact]
    public async Task Editar_WithStaleVersion_Returns409Concurrencia()
    {
        var token = await AdminTokenAsync();
        var leida = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);
        (await PutAsync(leida.Id, Body(leida, plazas: 11), token)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutAsync(leida.Id, Body(leida, plazas: 12), token);

        await ShouldBeProblemWithCodeAsync(response, 409, "concurrencia");
        (await GetAsync(leida.Id, token)).Plazas.Should().Be(11);
    }

    [Fact]
    public async Task Editar_SeatsBelowOccupied_Returns409WithOcupadas()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);
        await CrearReservasEnBdAsync(edicion.Id, pagadas: 3, pendientes: 2);

        var response = await PutAsync(edicion.Id, Body(edicion, plazas: 4), token);

        await ShouldBeProblemWithCodeAsync(response, 409, "plazas_por_debajo_de_ocupadas");
        (await ReadJsonAsync(response)).GetProperty("ocupadas").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task Editar_DateChangeWithBookings_RequiresConfirmation()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);
        await CrearReservasEnBdAsync(edicion.Id, pagadas: 2, pendientes: 1);
        var nuevaFecha = edicion.Inicio.AddDays(7);

        var sinConfirmar = await PutAsync(edicion.Id, Body(edicion, inicio: nuevaFecha), token);
        var confirmado = await PutAsync(edicion.Id, Body(edicion, inicio: nuevaFecha, confirmarCambioFecha: true), token);

        await ShouldBeProblemWithCodeAsync(sinConfirmar, 409, "cambio_fecha_requiere_confirmacion");
        (await ReadJsonAsync(sinConfirmar)).GetProperty("reservas").GetInt32().Should().Be(3);
        confirmado.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<EdicionEditadaTestDto>(confirmado)).Edicion.Inicio.Should().Be(nuevaFecha);
    }

    [Fact]
    public async Task Editar_PublishesEventOnlyFromHiddenToScheduled()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var oculta = await GetAsync((await CrearEdicionEnBdAsync(curso, Now.AddDays(20), estado: EstadoEdicion.Oculta)).Id, token);
        var programada = await GetAsync((await CrearEdicionEnBdAsync(curso, Now.AddDays(21))).Id, token);
        var aCancelar = await GetAsync((await CrearEdicionEnBdAsync(curso, Now.AddDays(22))).Id, token);

        (await PutAsync(oculta.Id, Body(oculta, estado: "Programada"), token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PutAsync(programada.Id, Body(programada, estado: "Oculta"), token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PutAsync(aCancelar.Id, Body(aCancelar, notas: "solo notas"), token)).StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelar = await GetAsync(aCancelar.Id, token);
        (await PutAsync(aCancelar.Id, Body(cancelar, estado: "Cancelada"), token)).StatusCode.Should().Be(HttpStatusCode.OK);

        EventRecorder.Count<EdicionPublicadaEvent>(e => e.EdicionId == oculta.Id).Should().Be(1);
        EventRecorder.Count<EdicionPublicadaEvent>(e => e.EdicionId == programada.Id).Should().Be(0);
        EventRecorder.Count<EdicionPublicadaEvent>(e => e.EdicionId == aCancelar.Id).Should().Be(0);
    }

    [Fact]
    public async Task Editar_CancellingWithBookings_WarnsAndCannotBeReactivated()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);
        await CrearReservasEnBdAsync(edicion.Id, pagadas: 1, pendientes: 1);

        var cancelar = await PutAsync(edicion.Id, Body(edicion, estado: "Cancelada"), token);
        var cancelada = (await ReadAsync<EdicionEditadaTestDto>(cancelar)).Edicion;
        var reactivar = await PutAsync(edicion.Id, Body(cancelada, estado: "Programada"), token);

        cancelar.StatusCode.Should().Be(HttpStatusCode.OK);
        cancelada.Estado.Should().Be("Cancelada");
        (await ReadAsync<EdicionEditadaTestDto>(await PutAsync(edicion.Id, Body(cancelada, notas: "x"), token)))
            .Edicion.Notas.Should().Be("x");
        await ShouldBeProblemWithCodeAsync(reactivar, 409, "estado_invalido");
    }

    [Fact]
    public async Task Editar_CancellingWithBookings_ReturnsWarning()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);
        await CrearReservasEnBdAsync(edicion.Id, pagadas: 2);

        var response = await PutAsync(edicion.Id, Body(edicion, estado: "Cancelada"), token);

        (await ReadAsync<EdicionEditadaTestDto>(response)).Avisos
            .Should().Equal("Tiene 2 inscritos: avisa y reembolsa manualmente");
    }

    [Fact]
    public async Task Editar_StartedEdition_OnlyAcceptsNotes()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(-2))).Id, token);

        var notas = await PutAsync(edicion.Id, Body(edicion, notas: "Fue genial"), token);
        var actualizada = (await ReadAsync<EdicionEditadaTestDto>(notas)).Edicion;
        var plazas = await PutAsync(edicion.Id, Body(actualizada, plazas: 20), token);

        notas.StatusCode.Should().Be(HttpStatusCode.OK);
        await ShouldBeProblemWithCodeAsync(plazas, 409, "estado_invalido");
    }

    [Fact]
    public async Task Editar_InvalidVersion_Returns400()
    {
        var token = await AdminTokenAsync();
        var edicion = await GetAsync((await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20))).Id, token);

        var response = await PutAsync(edicion.Id, Body(edicion, version: "abc"), token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Borrar_WithoutBookings_Returns204AndRemovesIt()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20));

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/ediciones/{edicion.Id}", accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(HttpMethod.Get, $"/api/admin/ediciones/{edicion.Id}", accessToken: token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Borrar_WithBookings_Returns409EdicionConReservas()
    {
        var token = await AdminTokenAsync();
        var edicion = await CrearEdicionEnBdAsync(await CrearCursoEnBdAsync(), Now.AddDays(20));
        await CrearReservasEnBdAsync(edicion.Id, expiradas: 1);

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/ediciones/{edicion.Id}", accessToken: token);

        await ShouldBeProblemWithCodeAsync(response, 409, "edicion_con_reservas");
    }

    [Fact]
    public async Task Borrar_Unknown_Returns404()
    {
        var response = await SendAsync(
            HttpMethod.Delete, $"/api/admin/ediciones/{Guid.NewGuid()}", accessToken: await AdminTokenAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

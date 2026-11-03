using System.Net;
using FluentAssertions;
using IntegrationTests.TestSupport;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Catalogo;

/// <summary>Anonymous catalogue endpoints: /api/cursos, /api/cursos/{slug}, /api/cursos/{slug}/ediciones.</summary>
public class CursosPublicosTests(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    [Fact]
    public async Task ListarCursos_AfterStartup_ContainsTheFourSeededCourses()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/cursos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cursos = await ReadAsync<List<CursoResumenTestDto>>(response);
        cursos.Select(c => c.Slug).Should().Contain(["curso-1", "curso-2", "curso-3", "curso-4"]);
        cursos.Select(c => c.Orden).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ListarCursos_OnlyReturnsActiveCourses()
    {
        var activo = await CrearCursoEnBdAsync();
        var inactivo = await CrearCursoEnBdAsync(activo: false);

        var cursos = await ReadAsync<List<CursoResumenTestDto>>(await SendAsync(HttpMethod.Get, "/api/cursos"));

        cursos.Should().Contain(c => c.Id == activo.Id);
        cursos.Should().NotContain(c => c.Id == inactivo.Id);
    }

    [Fact]
    public async Task ListarCursos_NextEdition_PrefersFreeSeatsThenFirstVisibleThenNull()
    {
        var conPlazas = await CrearCursoEnBdAsync();
        var completa = await CrearEdicionEnBdAsync(conPlazas, Now.AddDays(10), plazas: 2);
        var libre = await CrearEdicionEnBdAsync(conPlazas, Now.AddDays(20));
        TestOcupacionReader.Set(completa.Id, pagadas: 2);

        var todasCompletas = await CrearCursoEnBdAsync();
        var primera = await CrearEdicionEnBdAsync(todasCompletas, Now.AddDays(10), plazas: 1);
        var segunda = await CrearEdicionEnBdAsync(todasCompletas, Now.AddDays(20), plazas: 1);
        TestOcupacionReader.Set(primera.Id, pagadas: 1);
        TestOcupacionReader.Set(segunda.Id, pagadas: 0, pendientes: 1);

        var sinEdiciones = await CrearCursoEnBdAsync();
        await CrearEdicionEnBdAsync(sinEdiciones, Now.AddDays(10), estado: EstadoEdicion.Oculta);

        var cursos = await ReadAsync<List<CursoResumenTestDto>>(await SendAsync(HttpMethod.Get, "/api/cursos"));

        cursos.Single(c => c.Id == conPlazas.Id).ProximaEdicion!.Id.Should().Be(libre.Id);
        var proxima = cursos.Single(c => c.Id == todasCompletas.Id).ProximaEdicion!;
        proxima.Id.Should().Be(primera.Id);
        proxima.Estado.Should().Be("Completo");
        cursos.Single(c => c.Id == sinEdiciones.Id).ProximaEdicion.Should().BeNull();
    }

    [Fact]
    public async Task ObtenerCurso_ReturnsDetailWithVisibleEditionsByStart()
    {
        var curso = await CrearCursoEnBdAsync();
        var tarde = await CrearEdicionEnBdAsync(curso, Now.AddDays(40));
        var pronto = await CrearEdicionEnBdAsync(curso, Now.AddDays(4));

        var response = await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalle = await ReadAsync<CursoDetalleTestDto>(response);
        detalle.Descripcion.Should().Be("Larga");
        detalle.Precio.Should().Be(120m);
        detalle.Ediciones.Select(e => e.Id).Should().Equal(pronto.Id, tarde.Id);
        detalle.ProximaEdicion!.Id.Should().Be(pronto.Id);
    }

    [Fact]
    public async Task ObtenerCurso_UnknownOrInactive_Returns404NoEncontrado()
    {
        var inactivo = await CrearCursoEnBdAsync(activo: false);

        await ShouldBeProblemWithCodeAsync(await SendAsync(HttpMethod.Get, "/api/cursos/no-existe"), 404, "no_encontrado");
        await ShouldBeProblemWithCodeAsync(await SendAsync(HttpMethod.Get, $"/api/cursos/{inactivo.Slug}"), 404, "no_encontrado");
        await ShouldBeProblemWithCodeAsync(
            await SendAsync(HttpMethod.Get, $"/api/cursos/{inactivo.Slug}/ediciones"), 404, "no_encontrado");
    }

    [Fact]
    public async Task ListarEdiciones_OnlyReturnsVisibleEditions()
    {
        var curso = await CrearCursoEnBdAsync();
        var visible = await CrearEdicionEnBdAsync(curso, Now.AddDays(10));
        await CrearEdicionEnBdAsync(curso, Now.AddDays(11), estado: EstadoEdicion.Oculta);
        await CrearEdicionEnBdAsync(curso, Now.AddDays(12), estado: EstadoEdicion.Cancelada);
        await CrearEdicionEnBdAsync(curso, Now.AddHours(2)); // inside the 24 h closing window
        await CrearEdicionEnBdAsync(curso, Now.AddDays(-3)); // already started

        var response = await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}/ediciones");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<List<EdicionPublicaTestDto>>(response)).Should().ContainSingle().Which.Id.Should().Be(visible.Id);
    }

    [Fact]
    public async Task ListarEdiciones_ReportsFreeSeatsAndPublicState()
    {
        var curso = await CrearCursoEnBdAsync();
        var disponible = await CrearEdicionEnBdAsync(curso, Now.AddDays(10), plazas: 10);
        var ultimas = await CrearEdicionEnBdAsync(curso, Now.AddDays(11), plazas: 10);
        var completa = await CrearEdicionEnBdAsync(curso, Now.AddDays(12), plazas: 10);
        TestOcupacionReader.Set(ultimas.Id, pagadas: 5, pendientes: 2);
        TestOcupacionReader.Set(completa.Id, pagadas: 10);

        var ediciones = await ReadAsync<List<EdicionPublicaTestDto>>(
            await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}/ediciones"));

        ediciones.Select(e => (e.Id, e.PlazasTotales, e.PlazasLibres, e.Estado)).Should().Equal(
            (disponible.Id, 10, 10, "Disponible"),
            (ultimas.Id, 10, 3, "UltimasPlazas"),
            (completa.Id, 10, 0, "Completo"));
    }

    [Fact]
    public async Task ListarEdiciones_IsNeverCached()
    {
        var curso = await CrearCursoEnBdAsync();

        var response = await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}/ediciones");

        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }
}

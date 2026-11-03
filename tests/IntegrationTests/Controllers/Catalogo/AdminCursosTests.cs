using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers.Catalogo;

/// <summary>Course selector (Backoffice) and course management (SuperAdmin) under /api/admin/cursos.</summary>
public class AdminCursosTests(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    private static object Body(string slug, bool activo = true, string nombre = "Gel semipermanente") => new
    {
        nombre,
        slug,
        descripcionCorta = "Aprende gel",
        descripcion = "## Temario",
        precio = 189.90m,
        imagen = "/cursos/gel.jpg",
        ubicacion = "Madrid",
        duracionTexto = "1 día",
        activo,
        orden = 7
    };

    private async Task<string> SuperAdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.SuperAdmin)).AccessToken;

    private async Task<CursoAdminTestDto> CrearAsync(string token, string slug)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/admin/cursos", content: JsonContent.Create(Body(slug)), accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await ReadAsync<CursoAdminTestDto>(response);
    }

    [Fact]
    public async Task Listar_AsAdmin_ReturnsActiveAndInactiveCourses()
    {
        var activo = await CrearCursoEnBdAsync();
        var inactivo = await CrearCursoEnBdAsync(activo: false);
        var admin = await LoginAsNewUserAsync(UserRole.Admin);

        var response = await SendAsync(HttpMethod.Get, "/api/admin/cursos", accessToken: admin.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cursos = await ReadAsync<List<CursoOpcionTestDto>>(response);
        cursos.Should().ContainEquivalentOf(new CursoOpcionTestDto(activo.Id, activo.Nombre, activo.Slug, true));
        cursos.Should().ContainEquivalentOf(new CursoOpcionTestDto(inactivo.Id, inactivo.Nombre, inactivo.Slug, false));
    }

    [Fact]
    public async Task Obtener_AsSuperAdmin_ReturnsEveryFieldOfAnInactiveCourse()
    {
        var token = await SuperAdminTokenAsync();
        var creado = await CrearAsync(token, UniqueSlug());
        (await SendAsync(HttpMethod.Delete, $"/api/admin/cursos/{creado.Id}", accessToken: token))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await SendAsync(HttpMethod.Get, $"/api/admin/cursos/{creado.Id}", accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<CursoAdminTestDto>(response)).Should().Be(creado with { Activo = false });
    }

    [Fact]
    public async Task Obtener_AsAdminOrUnknown_Returns403Or404()
    {
        var curso = await CrearCursoEnBdAsync();
        var admin = await LoginAsNewUserAsync(UserRole.Admin);

        var comoAdmin = await SendAsync(HttpMethod.Get, $"/api/admin/cursos/{curso.Id}", accessToken: admin.AccessToken);
        var inexistente = await SendAsync(
            HttpMethod.Get, $"/api/admin/cursos/{Guid.NewGuid()}", accessToken: await SuperAdminTokenAsync());

        comoAdmin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeProblemWithCodeAsync(inexistente, 404, "no_encontrado");
    }

    [Fact]
    public async Task Crear_AsAdmin_Returns403()
    {
        var admin = await LoginAsNewUserAsync(UserRole.Admin);

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/cursos", content: JsonContent.Create(Body(UniqueSlug())), accessToken: admin.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Crear_AsSuperAdmin_Returns201AndIsPublic()
    {
        var token = await SuperAdminTokenAsync();
        var slug = UniqueSlug();

        var curso = await CrearAsync(token, slug);

        curso.Slug.Should().Be(slug);
        curso.Precio.Should().Be(189.90m);
        curso.Ubicacion.Should().Be("Madrid");
        (await SendAsync(HttpMethod.Get, $"/api/cursos/{slug}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Crear_DuplicateSlug_Returns400OnSlug()
    {
        var token = await SuperAdminTokenAsync();
        var existente = await CrearCursoEnBdAsync();

        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/cursos", content: JsonContent.Create(Body(existente.Slug)), accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("Slug", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Crear_InvalidFields_Returns400PerField()
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/admin/cursos",
            content: JsonContent.Create(new { nombre = "", slug = "Mal Slug", descripcionCorta = "c", descripcion = "d", precio = 0, imagen = "/i.jpg", activo = true, orden = 1 }),
            accessToken: await SuperAdminTokenAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        foreach (var field in new[] { "Nombre", "Slug", "Precio" })
        {
            errors.TryGetProperty(field, out _).Should().BeTrue(field);
        }
    }

    [Fact]
    public async Task Editar_ChangingSlug_WarnsAboutUrls()
    {
        var token = await SuperAdminTokenAsync();
        var curso = await CrearAsync(token, UniqueSlug());
        var nuevoSlug = UniqueSlug();

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/cursos/{curso.Id}", content: JsonContent.Create(Body(nuevoSlug, nombre: "Renombrado")),
            accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var editado = await ReadAsync<CursoEditadoTestDto>(response);
        editado.Curso.Slug.Should().Be(nuevoSlug);
        editado.Curso.Nombre.Should().Be("Renombrado");
        editado.Avisos.Should().ContainSingle().Which.Should().Contain("slug");
        (await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Editar_DeactivatingWithFutureScheduledEditions_Returns409()
    {
        var token = await SuperAdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        await CrearEdicionEnBdAsync(curso, Now.AddDays(30));

        var response = await SendAsync(
            HttpMethod.Put, $"/api/admin/cursos/{curso.Id}", content: JsonContent.Create(Body(curso.Slug, activo: false)),
            accessToken: token);

        await ShouldBeProblemWithCodeAsync(response, 409, "curso_con_ediciones");
    }

    [Fact]
    public async Task Desactivar_WithoutFutureScheduledEditions_Returns204AndHidesCourse()
    {
        var token = await SuperAdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        await CrearEdicionEnBdAsync(curso, Now.AddDays(30), estado: ZoiNailAcademy.Domain.Enums.EstadoEdicion.Oculta);
        await CrearEdicionEnBdAsync(curso, Now.AddDays(-30));

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/cursos/{curso.Id}", accessToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SendAsync(HttpMethod.Get, $"/api/cursos/{curso.Slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var cursos = await ReadAsync<List<CursoOpcionTestDto>>(
            await SendAsync(HttpMethod.Get, "/api/admin/cursos", accessToken: token));
        cursos.Single(c => c.Id == curso.Id).Activo.Should().BeFalse();
    }

    [Fact]
    public async Task Desactivar_WithFutureScheduledEditions_Returns409CursoConEdiciones()
    {
        var token = await SuperAdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        await CrearEdicionEnBdAsync(curso, Now.AddDays(30));

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/cursos/{curso.Id}", accessToken: token);

        await ShouldBeProblemWithCodeAsync(response, 409, "curso_con_ediciones");
    }

    [Fact]
    public async Task Desactivar_Unknown_Returns404()
    {
        var response = await SendAsync(
            HttpMethod.Delete, $"/api/admin/cursos/{Guid.NewGuid()}", accessToken: await SuperAdminTokenAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.Catalogo;

/// <summary>Helpers to create courses and editions directly in the database.</summary>
public abstract class CatalogoTestBase(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    protected DateTimeOffset Now => Factory.Clock.GetUtcNow();

    protected static string UniqueSlug() => $"curso-{Guid.NewGuid():N}";

    protected async Task<Curso> CrearCursoEnBdAsync(bool activo = true, int orden = 500)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var curso = Curso.Create("Curso de test", UniqueSlug(), "Corta", "Larga", 120m, "/cursos/test.jpg", null, null, true, orden);
        db.Cursos.Add(curso);
        if (!activo)
        {
            curso.Desactivar(tieneEdicionesFuturasProgramadas: false);
        }

        await db.SaveChangesAsync();
        return curso;
    }

    /// <summary>
    /// Creates an edition in the database (the course must be active). <paramref name="inicio"/> may be
    /// in the past: the domain is given a "now" one day before the start.
    /// </summary>
    protected async Task<Edicion> CrearEdicionEnBdAsync(
        Curso curso, DateTimeOffset inicio, int plazas = 10, EstadoEdicion estado = EstadoEdicion.Programada)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cursoBd = await db.Cursos.FindAsync(curso.Id) ?? throw new InvalidOperationException("Curso inexistente");
        var creadaEn = inicio.AddDays(-1);

        var edicion = Edicion.Create(
            cursoBd, inicio, plazas, estado == EstadoEdicion.Cancelada ? EstadoEdicion.Programada : estado, null, creadaEn);
        if (estado == EstadoEdicion.Cancelada)
        {
            edicion.Actualizar(edicion.Inicio, plazas, EstadoEdicion.Cancelada, null, 0, false, creadaEn);
        }

        db.Ediciones.Add(edicion);
        await db.SaveChangesAsync();
        return edicion;
    }

    // Read as string (buffered) so a test can read the same body more than once.
    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), JsonOptions);

    protected static async Task ShouldBeProblemWithCodeAsync(HttpResponseMessage response, int status, string code)
    {
        ((int)response.StatusCode).Should().Be(status, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be(code);
    }
}

public record EdicionPublicaTestDto(Guid Id, DateTimeOffset Inicio, int PlazasTotales, int PlazasLibres, string Estado);

public record CursoResumenTestDto(
    Guid Id, string Slug, string Nombre, string DescripcionCorta, decimal Precio, string Imagen, int Orden,
    EdicionPublicaTestDto? ProximaEdicion);

public record CursoDetalleTestDto(
    Guid Id, string Slug, string Nombre, decimal Precio, EdicionPublicaTestDto? ProximaEdicion, string Descripcion,
    List<EdicionPublicaTestDto> Ediciones);

public record EdicionAdminTestDto(
    Guid Id, Guid CursoId, string Curso, DateTimeOffset Inicio, int Plazas, int Pagadas, int Pendientes, int Libres,
    string Estado, bool Reservable, string? Notas, string Version);

public record EdicionEditadaTestDto(EdicionAdminTestDto Edicion, List<string> Avisos);

public record CursoOpcionTestDto(Guid Id, string Nombre, string Slug, bool Activo);

public record CursoAdminTestDto(
    Guid Id, string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
    string? Ubicacion, string? DuracionTexto, bool Activo, int Orden);

public record CursoEditadoTestDto(CursoAdminTestDto Curso, List<string> Avisos);

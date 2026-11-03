using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;
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

    private static int _contactoCounter;

    /// <summary>Unique valid Spanish mobile number (E.164) for test contacts.</summary>
    protected static string UniqueTelefono() => $"+346{Interlocked.Increment(ref _contactoCounter) % 100_000_000:D8}";

    /// <summary>
    /// Inserts bookings directly in the database. <paramref name="pendientesCaducadas"/> are still
    /// <c>Pendiente</c> but their <c>ExpiraEn</c> has passed, so they hold no seat.
    /// </summary>
    protected async Task CrearReservasEnBdAsync(
        Guid edicionId, int pagadas = 0, int pendientes = 0, int expiradas = 0, int canceladas = 0, int pendientesCaducadas = 0)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var edicion = await db.Ediciones.Include(e => e.Curso).SingleAsync(e => e.Id == edicionId);
        var now = Now;

        Reserva Nueva(DateTimeOffset expiraEn, DateTimeOffset creadaEn) => Reserva.Create(
            edicion, CodigoReserva.Generar(), MetodoPago.Stripe, "Reserva de test", $"{Guid.NewGuid():N}@mail.test",
            UniqueTelefono(), expiraEn, creadaEn);

        var reservas = new List<Reserva>();
        for (var i = 0; i < pagadas; i++) { var r = Nueva(now.AddMinutes(35), now); r.Pagar(now); reservas.Add(r); }
        for (var i = 0; i < pendientes; i++) { reservas.Add(Nueva(now.AddMinutes(35), now)); }
        for (var i = 0; i < expiradas; i++) { var r = Nueva(now.AddMinutes(35), now); r.Expirar(now); reservas.Add(r); }
        for (var i = 0; i < canceladas; i++) { var r = Nueva(now.AddMinutes(35), now); r.Cancelar(now); reservas.Add(r); }
        for (var i = 0; i < pendientesCaducadas; i++) { reservas.Add(Nueva(now.AddMinutes(-1), now.AddHours(-1))); }

        db.Reservas.AddRange(reservas);
        await db.SaveChangesAsync();
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

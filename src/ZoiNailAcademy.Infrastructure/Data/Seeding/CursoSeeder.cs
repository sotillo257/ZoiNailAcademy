using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Infrastructure.Data.Seeding;

/// <summary>
/// Inserts the initial courses of <c>Data/Seed/cursos.json</c> (embedded resource). A course is
/// inserted only when its slug does not exist; existing courses are never overwritten, so the
/// SuperAdmin's edits survive restarts.
/// </summary>
public class CursoSeeder(AppDbContext dbContext, ILogger<CursoSeeder> logger)
{
    public const string ResourceName = "ZoiNailAcademy.Infrastructure.Data.Seed.cursos.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var semillas = await LeerSemillasAsync(cancellationToken);
        var existentes = await dbContext.Cursos.Select(c => c.Slug).ToListAsync(cancellationToken);

        foreach (var s in semillas.Where(s => !existentes.Contains(s.Slug)))
        {
            dbContext.Cursos.Add(Curso.Create(
                s.Nombre, s.Slug, s.DescripcionCorta, s.Descripcion, s.Precio, s.Imagen, s.Ubicacion, s.DuracionTexto,
                s.Activo, s.Orden, s.PrecioReserva));
            logger.LogInformation("Curso sembrado: {Slug}", s.Slug);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<CursoSemilla>> LeerSemillasAsync(CancellationToken cancellationToken)
    {
        await using var stream = typeof(CursoSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"No se encontró el recurso embebido '{ResourceName}'.");
        return await JsonSerializer.DeserializeAsync<List<CursoSemilla>>(stream, JsonOptions, cancellationToken)
            ?? [];
    }

    private sealed record CursoSemilla(
        string Nombre, string Slug, string DescripcionCorta, string Descripcion, decimal Precio, string Imagen,
        string? Ubicacion, string? DuracionTexto, bool Activo, int Orden, decimal? PrecioReserva = null);
}

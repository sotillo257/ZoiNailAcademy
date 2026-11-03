using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Infrastructure.Data;
using ZoiNailAcademy.Infrastructure.Data.Seeding;

namespace IntegrationTests.Seeding;

public class CursoSeederTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task SeedAsync_RunningAgain_DoesNotDuplicateNorOverwriteCourses()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var curso = await db.Cursos.SingleAsync(c => c.Slug == "curso-4");
            curso.Actualizar("Editado por el SuperAdmin", curso.Slug, curso.DescripcionCorta, curso.Descripcion, 250m,
                curso.Imagen, curso.Ubicacion, curso.DuracionTexto, curso.Activo, curso.Orden, false);
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CursoSeeder>().SeedAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seeded = await db.Cursos.Where(c => c.Slug.StartsWith("curso-") && c.Slug.Length == 7).ToListAsync();
            seeded.Select(c => c.Slug).Should().BeEquivalentTo(["curso-1", "curso-2", "curso-3", "curso-4"]);
            var editado = seeded.Single(c => c.Slug == "curso-4");
            editado.Nombre.Should().Be("Editado por el SuperAdmin");
            editado.Precio.Should().Be(250m);
        }
    }
}

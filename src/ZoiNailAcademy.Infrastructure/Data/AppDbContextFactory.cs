using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZoiNailAcademy.Infrastructure.Data;

/// <summary>
/// Factory design-time para AppDbContext usada por las migraciones de EF Core.
/// Lee la cadena de conexión de una variable de entorno para no depender del arranque completo de la app.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Database=zoinailacademy_migrations;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}

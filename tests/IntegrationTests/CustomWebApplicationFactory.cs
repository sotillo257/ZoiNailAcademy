using IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MediatR;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using Testcontainers.PostgreSql;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@seed.test";
    public const string AdminPassword = "admin-seed-password";
    public const string SuperAdminEmail = "superadmin@seed.test";
    public const string SuperAdminPassword = "superadmin-seed-password";

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    /// <summary>
    /// Clock of the shared host. It starts at the real "now" because JWT lifetime validation uses
    /// the real clock; tests that move time forward create their own host with their own clock.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgreSqlContainer.GetConnectionString());
        builder.UseSetting("PasswordHasher:WorkFactor", "4");
        builder.UseSetting("Auth:CookieSecure", "true");
        builder.UseSetting("Admin:SeedEmail", AdminEmail);
        builder.UseSetting("Admin:SeedPassword", AdminPassword);
        builder.UseSetting("Admin:SeedNombre", "Clienta");
        builder.UseSetting("SuperAdmin:SeedEmail", SuperAdminEmail);
        builder.UseSetting("SuperAdmin:SeedPassword", SuperAdminPassword);
        builder.UseSetting("SuperAdmin:SeedNombre", "Desarrollador");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddTransient<IStartupFilter, ProtectedTestEndpointStartupFilter>();
            services.AddControllers().AddApplicationPart(typeof(TestAdminController).Assembly);

            services.AddScoped<OcupacionReader>();
            services.AddScoped<IOcupacionReader, TestOcupacionReader>();
            services.AddTransient<INotificationHandler<EdicionPublicadaEvent>, RecordingEdicionPublicadaHandler>();
        });
    }
}

using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Infrastructure.Authentication;
using ZoiNailAcademy.Infrastructure.Data;
using ZoiNailAcademy.Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ZoiNailAcademy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PasswordHasherOptions>(configuration.GetSection(PasswordHasherOptions.SectionName));

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.Configure<SuperAdminSeedOptions>(configuration.GetSection(SuperAdminSeedOptions.SectionName));
        services.AddScoped<UserSeeder>();

        services.AddSingleton<IAuthTokenSettings, AuthTokenSettings>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrEmpty(databaseUrl))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión. Configure 'ConnectionStrings:DefaultConnection' o la variable de entorno 'DATABASE_URL'.");
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }
}

using Microsoft.AspNetCore.Hosting;

namespace IntegrationTests.TestSupport;

public static class HostSettings
{
    /// <summary>Production environment with the JWT settings that appsettings.Development.json would otherwise provide.</summary>
    public static void UseProduction(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-not-for-production-use-1234567890");
        builder.UseSetting("Jwt:Issuer", "ZoiNailAcademy.Api");
        builder.UseSetting("Jwt:Audience", "ZoiNailAcademy.Client");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
    }
}

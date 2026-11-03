using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Seeding;

public class UserSeederTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>Starts a new host (which migrates and seeds) against the shared database.</summary>
    private HttpClient StartHost(string adminEmail, string adminPassword, bool resetAdminPassword = false)
    {
        var host = Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Admin:SeedEmail", adminEmail);
            b.UseSetting("Admin:SeedPassword", adminPassword);
            b.UseSetting("SuperAdmin:ResetAdminPassword", resetAdminPassword ? "true" : "false");
        });
        return host.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private async Task<HttpStatusCode> LoginStatusAsync(string email, string password)
    {
        var response = await PostAsJsonFromIpAsync("/api/auth/login", new { email, password }, NextTestClientIp());
        return response.StatusCode;
    }

    private async Task<int> CountUsersAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.CountAsync(u => u.Email == email);
    }

    [Fact]
    public async Task Seed_StartingTwice_DoesNotDuplicateNorChangePassword()
    {
        var email = UniqueEmail();
        using (StartHost(email.ToUpperInvariant(), "first-password-1")) { }

        using (StartHost(email, "second-password-2")) { }

        (await CountUsersAsync(email)).Should().Be(1);
        (await LoginStatusAsync(email, "first-password-1")).Should().Be(HttpStatusCode.OK);
        (await LoginStatusAsync(email, "second-password-2")).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Seed_WithResetAdminPassword_ChangesAdminPasswordAndRevokesSessions()
    {
        var email = UniqueEmail();
        using (StartHost(email, "first-password-1")) { }
        var session = await LoginAsync(email, "first-password-1");

        using (StartHost(email, "reset-password-2", resetAdminPassword: true)) { }

        (await LoginStatusAsync(email, "reset-password-2")).Should().Be(HttpStatusCode.OK);
        (await LoginStatusAsync(email, "first-password-1")).Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(session.RefreshCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Admin:SeedEmail", "")]
    [InlineData("Admin:SeedPassword", "")]
    [InlineData("SuperAdmin:SeedPassword", "short")]
    public void Seed_InProductionWithInvalidSettings_FailsStartup(string key, string value)
    {
        using var host = Factory.WithWebHostBuilder(b =>
        {
            TestSupport.HostSettings.UseProduction(b);
            b.UseSetting(key, value);
        });

        var act = () => host.CreateClient();

        act.Should().Throw<Exception>()
            .Where(e => e.ToString().Contains(key) && e.ToString().Contains(nameof(InvalidOperationException)));
    }
}

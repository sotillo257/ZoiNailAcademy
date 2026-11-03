using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ZoiNailAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Controllers;

public class AuthControllerRegisterTests : IntegrationTestBase
{
    public AuthControllerRegisterTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    [Fact]
    public async Task Register_WithNewEmail_Returns201WithTokensAndUser()
    {
        var email = UniqueEmail();
        var payload = new { email, password = "password123", displayName = "Jesús" };

        var response = await PostAsJsonFromIpAsync("/auth/register", payload, NextTestClientIp());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be(email);
        body.User.DisplayName.Should().Be("Jesús");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = UniqueEmail();
        var clientIp = NextTestClientIp();
        var payload = new { email, password = "password123", displayName = "Jesús" };
        await PostAsJsonFromIpAsync("/auth/register", payload, clientIp);

        var duplicatePayload = new
        {
            email = $"  {email.ToUpperInvariant()}  ",
            password = "password123",
            displayName = "Otro"
        };
        var response = await PostAsJsonFromIpAsync("/auth/register", duplicatePayload, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NeverPlaintext()
    {
        var email = UniqueEmail();
        var payload = new { email, password = "password123", displayName = "Jesús" };

        await PostAsJsonFromIpAsync("/auth/register", payload, NextTestClientIp());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        user.PasswordHash.Should().NotBe("password123");
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Controllers;

public class AuthControllerRefreshTests : IntegrationTestBase
{
    public AuthControllerRefreshTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    private async Task<AuthResponseDto> RegisterUserAsync(string clientIp)
    {
        var payload = new { email = UniqueEmail(), password = "password123", displayName = "Jesús" };
        var response = await PostAsJsonFromIpAsync("/auth/register", payload, clientIp);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions))!;
    }

    [Fact]
    public async Task Refresh_WithActiveToken_RotatesAndReturnsNewPair()
    {
        var clientIp = NextTestClientIp();
        var auth = await RegisterUserAsync(clientIp);

        var response = await PostAsJsonFromIpAsync("/auth/refresh", new { refreshToken = auth.RefreshToken }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenPairResponseDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ReusingAlreadyRotatedToken_Returns401()
    {
        var clientIp = NextTestClientIp();
        var auth = await RegisterUserAsync(clientIp);
        await PostAsJsonFromIpAsync("/auth/refresh", new { refreshToken = auth.RefreshToken }, clientIp);

        var response = await PostAsJsonFromIpAsync("/auth/refresh", new { refreshToken = auth.RefreshToken }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var clientIp = NextTestClientIp();

        var response = await PostAsJsonFromIpAsync(
            "/auth/refresh", new { refreshToken = "does-not-exist" }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        var clientIp = NextTestClientIp();
        var rawToken = $"expired-raw-token-{Guid.NewGuid():N}";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();

            var user = User.Create(UniqueEmail(), "Jesús", "hash");
            db.Users.Add(user);

            var expiredToken = RefreshToken.Create(user.Id, hasher.Hash(rawToken), DateTime.UtcNow.AddSeconds(-1));
            db.RefreshTokens.Add(expiredToken);

            await db.SaveChangesAsync();
        }

        var response = await PostAsJsonFromIpAsync("/auth/refresh", new { refreshToken = rawToken }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

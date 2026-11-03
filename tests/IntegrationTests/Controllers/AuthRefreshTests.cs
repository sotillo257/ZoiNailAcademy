using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers;

public class AuthRefreshTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsNewTokenAndRotatesCookie()
    {
        var login = await LoginAsNewUserAsync(UserRole.Admin);

        var response = await RefreshAsync(login.RefreshCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions))!;
        body.AccessToken.Should().NotBeNullOrEmpty();
        body.User.Id.Should().Be(login.Body.User.Id);
        var newCookie = GetRefreshCookie(response);
        newCookie.Should().NotBeNullOrEmpty().And.NotBe(login.RefreshCookie);
        GetRefreshSetCookieHeader(response)!.ToLowerInvariant().Should().Contain("path=/api/auth").And.Contain("httponly");
    }

    [Fact]
    public async Task Refresh_ReusingRotatedCookie_Returns401AndRevokesTheNewOne()
    {
        var login = await LoginAsNewUserAsync(UserRole.Admin);
        var rotated = await RefreshAsync(login.RefreshCookie);
        var newCookie = GetRefreshCookie(rotated);

        var reuse = await RefreshAsync(login.RefreshCookie);
        var afterTheft = await RefreshAsync(newCookie);

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        reuse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        afterTheft.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await RefreshAsync(cookie: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Refresh_WithUnknownCookie_Returns401()
    {
        var response = await RefreshAsync("does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_IgnoresTokenInBody()
    {
        var login = await LoginAsNewUserAsync(UserRole.Admin);

        var response = await PostAsJsonFromIpAsync(
            "/api/auth/refresh", new { refreshToken = login.RefreshCookie }, NextTestClientIp());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterRefreshLifetimeElapsed_Returns401()
    {
        // Own host and clock, so moving time forward does not affect the shared fixture.
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var host = Factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(clock)));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await LoginAsync(await CreateUserAsync(UserRole.Admin), client: client);

        clock.Advance(TimeSpan.FromDays(13));
        var stillValid = await SendAsync(HttpMethod.Post, "/api/auth/refresh", refreshCookie: login.RefreshCookie, client: client);
        clock.Advance(TimeSpan.FromDays(14).Add(TimeSpan.FromSeconds(1)));
        var expired = await SendAsync(
            HttpMethod.Post, "/api/auth/refresh", refreshCookie: GetRefreshCookie(stillValid), client: client);

        stillValid.StatusCode.Should().Be(HttpStatusCode.OK);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_TwoConcurrentRequestsWithSameCookie_OnlyOneSucceeds()
    {
        var login = await LoginAsNewUserAsync(UserRole.Admin);

        var responses = await Task.WhenAll(RefreshAsync(login.RefreshCookie), RefreshAsync(login.RefreshCookie));

        responses.Select(r => r.StatusCode).Should()
            .BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Unauthorized]);
    }
}

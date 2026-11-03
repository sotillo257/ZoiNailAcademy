using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ZoiNailAcademy.Domain.Enums;

namespace IntegrationTests.Controllers;

/// <summary>Logout, me, cambiar-password and the removed register endpoint.</summary>
public class AuthSessionTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Logout_WithCookie_Returns204DeletesCookieAndRevokesIt()
    {
        var login = await LoginAsNewUserAsync(UserRole.Admin);

        var logout = await SendAsync(HttpMethod.Post, "/api/auth/logout", refreshCookie: login.RefreshCookie);
        var refresh = await RefreshAsync(login.RefreshCookie);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var setCookie = GetRefreshSetCookieHeader(logout)!.ToLowerInvariant();
        setCookie.Should().StartWith("rt=;").And.Contain("path=/api/auth").And.Contain("expires=thu, 01 jan 1970");
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutCookie_Returns204()
    {
        var logout = await SendAsync(HttpMethod.Post, "/api/auth/logout");

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUser()
    {
        var login = await LoginAsNewUserAsync(UserRole.SuperAdmin);

        var response = await SendAsync(HttpMethod.Get, "/api/auth/me", accessToken: login.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>(JsonOptions);
        user.Should().Be(login.Body.User);
        user!.Rol.Should().Be("SuperAdmin");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Success_KeepsOnlyCurrentSessionAndSwapsPassword()
    {
        var email = await CreateUserAsync(UserRole.Admin, "old-password-1");
        var sessionA = await LoginAsync(email, "old-password-1");
        var sessionB = await LoginAsync(email, "old-password-1");

        var change = await SendAsync(
            HttpMethod.Post, "/api/auth/cambiar-password",
            content: JsonContent.Create(new { passwordActual = "old-password-1", passwordNueva = "new-password-1" }),
            refreshCookie: sessionA.RefreshCookie, accessToken: sessionA.AccessToken);

        change.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await RefreshAsync(sessionA.RefreshCookie)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(sessionB.RefreshCookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostAsJsonFromIpAsync("/api/auth/login", new { email, password = "old-password-1" }, NextTestClientIp()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostAsJsonFromIpAsync("/api/auth/login", new { email, password = "new-password-1" }, NextTestClientIp()))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400AndKeepsPassword()
    {
        var email = await CreateUserAsync(UserRole.Admin, "old-password-1");
        var session = await LoginAsync(email, "old-password-1");

        var change = await SendAsync(
            HttpMethod.Post, "/api/auth/cambiar-password",
            content: JsonContent.Create(new { passwordActual = "not-my-password", passwordNueva = "new-password-1" }),
            accessToken: session.AccessToken);

        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await change.Content.ReadAsStringAsync()).Should().Contain("PasswordActual");
        (await PostAsJsonFromIpAsync("/api/auth/login", new { email, password = "old-password-1" }, NextTestClientIp()))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_Returns400()
    {
        var session = await LoginAsNewUserAsync(UserRole.Admin);

        var change = await SendAsync(
            HttpMethod.Post, "/api/auth/cambiar-password",
            content: JsonContent.Create(new { passwordActual = "password-123", passwordNueva = "short" }),
            accessToken: session.AccessToken);

        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await change.Content.ReadAsStringAsync()).Should().Contain("PasswordNueva");
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var change = await SendAsync(
            HttpMethod.Post, "/api/auth/cambiar-password",
            content: JsonContent.Create(new { passwordActual = "password-123", passwordNueva = "new-password-1" }));

        change.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_DoesNotExist()
    {
        var session = await LoginAsNewUserAsync(UserRole.SuperAdmin);
        var payload = new { email = UniqueEmail(), password = "password-123", displayName = "X" };

        var authenticated = await SendAsync(
            HttpMethod.Post, "/api/auth/register", content: JsonContent.Create(payload), accessToken: session.AccessToken);
        var anonymous = await PostAsJsonFromIpAsync("/api/auth/register", payload, NextTestClientIp());

        authenticated.StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Unmatched routes fall under the global fallback policy, so anonymous callers see 401.
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        RouteTests.GetRouteEndpoints(Factory).Should().NotContain(e => (e.RoutePattern.RawText ?? "").Contains("register"));
    }
}

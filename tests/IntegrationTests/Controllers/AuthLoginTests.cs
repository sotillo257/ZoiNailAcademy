using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Authentication;

namespace IntegrationTests.Controllers;

public class AuthLoginTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenUserAndNoRefreshTokenInBody()
    {
        var email = await CreateUserAsync(UserRole.Admin);

        var response = await PostAsJsonFromIpAsync(
            "/api/auth/login", new { email = email.ToUpperInvariant(), password = "password-123" }, NextTestClientIp());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContainEquivalentOf("refreshToken");
        var body = (await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions))!;
        body.AccessToken.Should().NotBeNullOrEmpty();
        var jwt = Factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        body.ExpiresIn.Should().Be(jwt.AccessTokenMinutes * 60);
        body.User.Email.Should().Be(email);
        body.User.Nombre.Should().Be("Usuario de test");
        body.User.Rol.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_Success_SetsRefreshCookieWithSecureAttributes()
    {
        var email = await CreateUserAsync(UserRole.Admin);

        var response = await PostAsJsonFromIpAsync(
            "/api/auth/login", new { email, password = "password-123" }, NextTestClientIp());

        var setCookie = GetRefreshSetCookieHeader(response);
        setCookie.Should().NotBeNull();
        var attributes = setCookie!.Split(';').Select(a => a.Trim().ToLowerInvariant()).ToList();
        attributes.Should().Contain(["httponly", "secure", "samesite=strict", "path=/api/auth"]);

        var expires = DateTimeOffset.Parse(attributes.Single(a => a.StartsWith("expires=", StringComparison.Ordinal))["expires=".Length..]);
        var refreshDays = Factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.RefreshTokenDays;
        expires.Should().BeCloseTo(Factory.Clock.GetUtcNow().AddDays(refreshDays), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_SeededUsers_CanLogInWithTheirRoles()
    {
        var admin = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var superAdmin = await LoginAsync(CustomWebApplicationFactory.SuperAdminEmail, CustomWebApplicationFactory.SuperAdminPassword);

        admin.Body.User.Rol.Should().Be("Admin");
        admin.Body.User.Nombre.Should().Be("Clienta");
        superAdmin.Body.User.Rol.Should().Be("SuperAdmin");
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameGeneric401()
    {
        var email = await CreateUserAsync(UserRole.Admin);

        var wrongPassword = await PostAsJsonFromIpAsync(
            "/api/auth/login", new { email, password = "wrong-password" }, NextTestClientIp());
        var unknownEmail = await PostAsJsonFromIpAsync(
            "/api/auth/login", new { email = UniqueEmail(), password = "password-123" }, NextTestClientIp());

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassword.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var p1 = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        var p2 = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        p1!.Detail.Should().Be("Email o contraseña incorrectos.").And.Be(p2!.Detail);
        GetRefreshSetCookieHeader(wrongPassword).Should().BeNull();
    }

    [Fact]
    public async Task Login_WithEmptyFields_Returns400()
    {
        var response = await PostAsJsonFromIpAsync(
            "/api/auth/login", new { email = "", password = "" }, NextTestClientIp());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

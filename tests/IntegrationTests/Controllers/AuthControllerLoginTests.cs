using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests.Controllers;

public class AuthControllerLoginTests : IntegrationTestBase
{
    public AuthControllerLoginTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    private async Task RegisterUserAsync(string email, string password, string clientIp)
    {
        var payload = new { email, password, displayName = "Jesús" };
        await PostAsJsonFromIpAsync("/auth/register", payload, clientIp);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithTokensAndUser()
    {
        var clientIp = NextTestClientIp();
        var email = UniqueEmail();
        await RegisterUserAsync(email, "password123", clientIp);

        var response = await PostAsJsonFromIpAsync("/auth/login", new { email, password = "password123" }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.User.Email.Should().Be(email);
        body.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGeneric401()
    {
        var clientIp = NextTestClientIp();
        var email = UniqueEmail();
        await RegisterUserAsync(email, "password123", clientIp);

        var response = await PostAsJsonFromIpAsync("/auth/login", new { email, password = "wrong-password" }, clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameGeneric401()
    {
        var clientIp = NextTestClientIp();

        var response = await PostAsJsonFromIpAsync(
            "/auth/login",
            new { email = UniqueEmail(), password = "password123" },
            clientIp);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnSameMessage()
    {
        var clientIp1 = NextTestClientIp();
        var clientIp2 = NextTestClientIp();
        var email = UniqueEmail();
        await RegisterUserAsync(email, "password123", clientIp1);

        var wrongPasswordResponse = await PostAsJsonFromIpAsync(
            "/auth/login", new { email, password = "wrong-password" }, clientIp1);
        var unknownEmailResponse = await PostAsJsonFromIpAsync(
            "/auth/login", new { email = UniqueEmail(), password = "password123" }, clientIp2);

        var wrongPasswordProblem = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        var unknownEmailProblem = await unknownEmailResponse.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);

        wrongPasswordProblem!.Detail.Should().Be(unknownEmailProblem!.Detail);
    }
}

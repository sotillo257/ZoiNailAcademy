using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests.Controllers;

public class AuthorizationTests : IntegrationTestBase
{
    public AuthorizationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401ProblemJson()
    {
        var response = await Client.GetAsync("/__test/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ResolvesSameUserIdAsSubClaim()
    {
        var clientIp = NextTestClientIp();
        var email = UniqueEmail();
        var registerResponse = await PostAsJsonFromIpAsync(
            "/auth/register",
            new { email, password = "password123", displayName = "Jesús" },
            clientIp);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions))!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/__test/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProtectedEndpointResponseDto>(JsonOptions);
        body!.UserId.Should().Be(auth.User.Id.ToString());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__test/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");
        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_IsReachableWithoutToken()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthEndpoints_AreReachableWithoutToken()
    {
        var response = await PostAsJsonFromIpAsync(
            "/auth/login",
            new { email = UniqueEmail(), password = "password123" },
            NextTestClientIp());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        problem!.Detail.Should().Be("Email o contraseña incorrectos.");
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Host.Authorization;

namespace IntegrationTests.Controllers;

public partial class AuthorizationTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly string[] BackofficePolicies = [AuthorizationPolicies.Backoffice, AuthorizationPolicies.SuperAdmin];

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();

    /// <summary>Every /api/admin endpoint, discovered dynamically (not a hand-written list).</summary>
    private List<(string Method, string Url, RouteEndpoint Endpoint)> AdminEndpoints() =>
        RouteTests.GetRouteEndpoints(Factory)
            .Where(e => RouteTests.Route(e).StartsWith("api/admin", StringComparison.Ordinal))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(method => (method, "/" + RouteParameter().Replace(RouteTests.Route(e), Guid.NewGuid().ToString()), e)))
            .ToList();

    private static bool RequiresSuperAdmin(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Policy == AuthorizationPolicies.SuperAdmin);

    private Task<HttpResponseMessage> CallAsync((string Method, string Url, RouteEndpoint _) endpoint, string? token) =>
        SendAsync(new HttpMethod(endpoint.Method), endpoint.Url, content: JsonContent.Create(new { }), accessToken: token);

    [Fact]
    public void AdminEndpoints_AllRequireABackofficePolicyAndNoneIsAnonymous()
    {
        var endpoints = AdminEndpoints();

        endpoints.Should().NotBeEmpty("the test admin controller is always registered");
        foreach (var (method, url, endpoint) in endpoints)
        {
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().BeNull($"{method} {url} must not allow anonymous access");
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Should().Contain(a => BackofficePolicies.Contains(a.Policy), $"{method} {url} must require the Backoffice policy");
        }
    }

    [Fact]
    public async Task AdminEndpoints_WithoutToken_Return401()
    {
        foreach (var endpoint in AdminEndpoints())
        {
            var response = await CallAsync(endpoint, token: null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"{endpoint.Method} {endpoint.Url}");
        }
    }

    [Fact]
    public async Task SuperAdminEndpoints_WithAdminToken_Return403AndAllowSuperAdmin()
    {
        var admin = await LoginAsNewUserAsync(UserRole.Admin);
        var superAdmin = await LoginAsNewUserAsync(UserRole.SuperAdmin);
        var superAdminEndpoints = AdminEndpoints().Where(e => RequiresSuperAdmin(e.Endpoint)).ToList();

        superAdminEndpoints.Should().NotBeEmpty();
        foreach (var endpoint in superAdminEndpoints)
        {
            var asAdmin = await CallAsync(endpoint, admin.AccessToken);
            var asSuperAdmin = await CallAsync(endpoint, superAdmin.AccessToken);

            asAdmin.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{endpoint.Method} {endpoint.Url}");
            asAdmin.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            asSuperAdmin.StatusCode.Should().NotBe(HttpStatusCode.Forbidden).And.NotBe(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task BackofficeEndpoint_WithAdminToken_IsAllowed()
    {
        var admin = await LoginAsNewUserAsync(UserRole.Admin);

        var response = await SendAsync(HttpMethod.Get, "/api/admin/__test", accessToken: admin.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401ProblemJson()
    {
        var response = await Client.GetAsync("/api/__test/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_Returns401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/__test/protected", accessToken: "not-a-valid-jwt");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ResolvesUserIdFromSubAndCarriesRole()
    {
        var login = await LoginAsNewUserAsync(UserRole.SuperAdmin);

        var response = await SendAsync(HttpMethod.Get, "/api/__test/protected", accessToken: login.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProtectedResponse>(JsonOptions);
        body!.UserId.Should().Be(login.Body.User.Id.ToString());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        jwt.Claims.Should().ContainSingle(c => c.Type == "role").Which.Value.Should().Be("SuperAdmin");
    }

    private sealed record ProtectedResponse(string? UserId);
}

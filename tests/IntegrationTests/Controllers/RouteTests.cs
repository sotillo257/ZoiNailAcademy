using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Controllers;

public class RouteTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    public static IReadOnlyList<RouteEndpoint> GetRouteEndpoints(CustomWebApplicationFactory factory) =>
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

    public static string Route(RouteEndpoint endpoint) => (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');

    [Fact]
    public void AllEndpoints_AreUnderApiPrefix()
    {
        var endpoints = GetRouteEndpoints(Factory);

        endpoints.Should().NotBeEmpty();
        endpoints
            .Select(Route)
            // The OpenAPI document is development tooling (Swagger UI), never reached through the Next rewrite.
            .Where(route => !route.StartsWith("openapi/", StringComparison.Ordinal))
            .Should().OnlyContain(route => route.StartsWith("api/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Health_UnderApi_Returns200WithoutToken()
    {
        var response = await Client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_WithoutPrefix_IsNotMapped()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}

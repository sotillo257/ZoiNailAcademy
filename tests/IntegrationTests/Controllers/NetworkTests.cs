using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace IntegrationTests.Controllers;

/// <summary>Forwarded headers, IP diagnostic endpoint and CORS.</summary>
public class NetworkTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DiagIp_DisabledByDefault_Returns404()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/_diag/ip");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DiagIp_WhenEnabled_ReportsForwardedClientIp()
    {
        using var host = Factory.WithWebHostBuilder(b => b.UseSetting("Diag:Enabled", "true"));
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/_diag/ip");
        request.Headers.Add("X-Forwarded-For", "198.51.100.7");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        body["remoteIpAddress"]!.GetValue<string>().Should().Be("198.51.100.7");
        body["scheme"]!.GetValue<string>().Should().Be("https");
    }

    [Fact]
    public async Task Cors_InDevelopment_AllowsConfiguredOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await Client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle("http://localhost:3000");
    }

    [Fact]
    public async Task Cors_InProduction_EmitsNoCorsHeaders()
    {
        using var host = Factory.WithWebHostBuilder(TestSupport.HostSettings.UseProduction);
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}

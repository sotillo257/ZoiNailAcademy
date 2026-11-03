using System.Net;
using FluentAssertions;

namespace IntegrationTests.Controllers;

public class RateLimitingTests : IntegrationTestBase
{
    public RateLimitingTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SixthRequestWithinAMinute_FromSameForwardedIp_Returns429()
    {
        var clientIp = NextTestClientIp();
        var payload = new { email = $"user-{Guid.NewGuid():N}@mail.com", password = "password123" };

        for (var i = 0; i < 5; i++)
        {
            await PostAsJsonFromIpAsync("/auth/login", payload, clientIp);
        }

        var sixthResponse = await PostAsJsonFromIpAsync("/auth/login", payload, clientIp);

        sixthResponse.StatusCode.Should().Be((HttpStatusCode)429);
        sixthResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task RequestsFromDifferentForwardedIps_AreNotThrottledTogether()
    {
        var payload = new { email = $"user-{Guid.NewGuid():N}@mail.com", password = "password123" };

        for (var i = 0; i < 5; i++)
        {
            await PostAsJsonFromIpAsync("/auth/login", payload, NextTestClientIp());
        }

        var response = await PostAsJsonFromIpAsync("/auth/login", payload, NextTestClientIp());

        response.StatusCode.Should().NotBe((HttpStatusCode)429);
    }
}

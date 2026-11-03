using System.Net;
using FluentAssertions;
using IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZoiNailAcademy.Host.RateLimiting;

namespace IntegrationTests.Controllers;

public class RateLimitingTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private RateLimitingOptions Options => Factory.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    [Fact]
    public async Task Login_RequestNPlusOneFromSameIp_Returns429WithRetryAfter()
    {
        var clientIp = NextTestClientIp();
        var payload = new { email = UniqueEmail(), password = "password-123" };
        var limit = Options.Auth.PermitLimit;

        for (var i = 0; i < limit; i++)
        {
            (await PostAsJsonFromIpAsync("/api/auth/login", payload, clientIp))
                .StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var rejected = await PostAsJsonFromIpAsync("/api/auth/login", payload, clientIp);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        rejected.Headers.RetryAfter.Should().NotBeNull();
        rejected.Headers.RetryAfter!.Delta.Should().BePositive().And.BeLessThanOrEqualTo(Options.Auth.Window);
    }

    [Fact]
    public async Task Login_FromDifferentForwardedIps_IsNotThrottledTogether()
    {
        var payload = new { email = UniqueEmail(), password = "password-123" };

        for (var i = 0; i <= Options.Auth.PermitLimit; i++)
        {
            (await PostAsJsonFromIpAsync("/api/auth/login", payload, NextTestClientIp()))
                .StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public void DefaultLimits_MatchTheSpec()
    {
        Options.Auth.PermitLimit.Should().Be(10);
        Options.Auth.Window.Should().Be(TimeSpan.FromMinutes(5));
        Options.Reservas.PermitLimit.Should().Be(10);
        Options.Reservas.Window.Should().Be(TimeSpan.FromMinutes(10));
        Options.Formularios.PermitLimit.Should().Be(5);
        Options.Formularios.Window.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Theory]
    [InlineData(RateLimitPolicies.Auth)]
    [InlineData(RateLimitPolicies.Reservas)]
    [InlineData(RateLimitPolicies.Formularios)]
    public async Task NamedPolicy_ThrottlesAfterItsPermitLimit(string policy)
    {
        var limit = policy switch
        {
            RateLimitPolicies.Auth => Options.Auth.PermitLimit,
            RateLimitPolicies.Reservas => Options.Reservas.PermitLimit,
            _ => Options.Formularios.PermitLimit
        };
        ProtectedTestEndpointStartupFilter.RateLimitPolicyNames.Should().Contain(policy);
        var clientIp = NextTestClientIp();

        for (var i = 0; i < limit; i++)
        {
            (await SendAsync(HttpMethod.Get, $"/api/__test/rate/{policy}", clientIp)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rejected = await SendAsync(HttpMethod.Get, $"/api/__test/rate/{policy}", clientIp);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.Should().NotBeNull();
    }
}

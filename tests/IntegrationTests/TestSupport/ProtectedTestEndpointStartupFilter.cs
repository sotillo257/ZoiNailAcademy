using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ZoiNailAcademy.Host.RateLimiting;

namespace IntegrationTests.TestSupport;

/// <summary>
/// Adds throwaway endpoints for the tests: one with no [Authorize]/[AllowAnonymous] metadata (to
/// verify the global FallbackPolicy) and one anonymous endpoint per named rate-limit policy.
/// </summary>
public class ProtectedTestEndpointStartupFilter : IStartupFilter
{
    public static readonly string[] RateLimitPolicyNames =
        [RateLimitPolicies.Auth, RateLimitPolicies.Reservas, RateLimitPolicies.Formularios];

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/api/__test/protected", (HttpContext context) =>
                Results.Ok(new { userId = context.User.FindFirst("sub")?.Value }));

            foreach (var policy in RateLimitPolicyNames)
            {
                endpoints.MapGet($"/api/__test/rate/{policy}", () => Results.Ok())
                    .RequireRateLimiting(policy)
                    .AllowAnonymous();
            }
        });
    };
}

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.TestSupport;

/// <summary>
/// Adds a throwaway endpoint with no [Authorize]/[AllowAnonymous] metadata, so integration
/// tests can verify the global FallbackPolicy independently of any business endpoint.
/// </summary>
public class ProtectedTestEndpointStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/__test/protected", (HttpContext context) =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;

                return Results.Ok(new { userId });
            });
        });
    };
}

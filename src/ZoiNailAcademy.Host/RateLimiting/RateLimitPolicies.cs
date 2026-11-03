using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ZoiNailAcademy.Host.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary><c>/api/auth/*</c>.</summary>
    public const string Auth = "auth";

    /// <summary>Public booking endpoints (F03, F04).</summary>
    public const string Reservas = "reservas";

    /// <summary>Public forms: waiting list and contact (F05, F06).</summary>
    public const string Formularios = "formularios";

    public static void AddPolicies(RateLimiterOptions options, RateLimitingOptions settings)
    {
        options.OnRejected = WriteProblemAsync;

        AddFixedWindowPerIp(options, Auth, settings.Auth);
        AddFixedWindowPerIp(options, Reservas, settings.Reservas);
        AddFixedWindowPerIp(options, Formularios, settings.Formularios);
    }

    private static void AddFixedWindowPerIp(RateLimiterOptions options, string name, RateLimitPolicyOptions policy) =>
        options.AddPolicy(name, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                // RemoteIpAddress is the real client IP once ForwardedHeaders has run (first middleware).
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    QueueLimit = 0
                }));

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Se ha superado el límite de peticiones. Inténtalo de nuevo más tarde."
        };

        await response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);
    }
}

public class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
}

/// <summary>Section <c>RateLimit</c>, e.g. <c>RateLimit__Reservas__PermitLimit</c>, <c>RateLimit__Reservas__Window=00:10:00</c>.</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimit";

    public RateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(5) };
    public RateLimitPolicyOptions Reservas { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(10) };
    public RateLimitPolicyOptions Formularios { get; set; } = new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) };
}

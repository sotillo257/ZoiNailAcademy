using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>
/// Temporary endpoint to check, on Railway, that the API sees the real client IP behind the edge
/// and the Next.js rewrite. Answers 404 unless <c>Diag:Enabled=true</c>; disable it after checking.
/// </summary>
[ApiController]
[Route("_diag")]
[AllowAnonymous]
public class DiagController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("ip")]
    public IActionResult GetIp()
    {
        if (!configuration.GetValue<bool>("Diag:Enabled"))
        {
            return NotFound();
        }

        var headers = Request.Headers
            .Where(h => h.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase)
                     || h.Key.StartsWith("X-Original-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return Ok(new
        {
            remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            scheme = Request.Scheme,
            headers
        });
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.Emails.PrevisualizarEmail;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Email previews with sample data, to review the texts with the client before launch.</summary>
[ApiController]
[Route("admin/emails")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public class AdminEmailsController(ISender sender) : ControllerBase
{
    /// <param name="tipo">One of <see cref="TiposEmail.Todos"/>, e.g. <c>ReservaPagada.Comprador</c>.</param>
    /// <param name="formato"><c>html</c> (default) or <c>texto</c> for the plain-text version.</param>
    [HttpGet("preview/{tipo}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/html")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(string tipo, [FromQuery] string? formato, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PrevisualizarEmailQuery(tipo), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Email-Asunto"] = Uri.EscapeDataString(result.Value.Asunto);
        return string.Equals(formato, "texto", StringComparison.OrdinalIgnoreCase)
            ? Content(result.Value.Texto, "text/plain; charset=utf-8")
            : Content(result.Value.Html, "text/html; charset=utf-8");
    }
}

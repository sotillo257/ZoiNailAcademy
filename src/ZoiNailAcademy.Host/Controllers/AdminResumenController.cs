using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Resumen.ObtenerResumen;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Counters for the home page of the backoffice.</summary>
[ApiController]
[Route("admin/resumen")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminResumenController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResumenDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObtenerResumenQuery(), cancellationToken);
        return Ok(result.Value);
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;
using ZoiNailAcademy.Host.RateLimiting;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Public waiting list form.</summary>
[ApiController]
[Route("lista-espera")]
[AllowAnonymous]
public class ListaEsperaController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Joins the waiting list of a course. Signing up again with the same email updates the entry and answers
    /// the same (it never reveals whether someone was already on the list).
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Formularios)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Alta(AltaListaEsperaCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }
}

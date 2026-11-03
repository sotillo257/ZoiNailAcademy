using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;
using ZoiNailAcademy.Application.ListasEspera.DarseDeBaja;
using ZoiNailAcademy.Host.RateLimiting;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Public waiting list form and unsubscribe from the new-dates emails.</summary>
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

    /// <summary>
    /// Unsubscribes the entry that owns <c>token</c> from the new-dates emails of its course. The token comes in the
    /// query (<c>?token=</c>, the one-click <c>List-Unsubscribe</c> URL: Gmail posts the form body
    /// <c>List-Unsubscribe=One-Click</c>), in a JSON body <c>{ "token": "…" }</c> (the front's <c>/baja</c> page) or in a
    /// form body field <c>token</c>. Always 204, valid or not, so it never reveals who is on a waiting list.
    /// </summary>
    [HttpPost("baja")]
    [EnableRateLimiting(RateLimitPolicies.Formularios)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Baja([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            token = await LeerTokenDelCuerpoAsync(cancellationToken);
        }

        await sender.Send(new DarseDeBajaCommand(token), cancellationToken);
        return NoContent();
    }

    private async Task<string?> LeerTokenDelCuerpoAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Request.HasJsonContentType())
            {
                return (await Request.ReadFromJsonAsync<BajaRequest>(cancellationToken))?.Token;
            }

            if (Request.HasFormContentType)
            {
                return (await Request.ReadFormAsync(cancellationToken))["token"].FirstOrDefault();
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or BadHttpRequestException)
        {
            // A malformed body is answered like an invalid token.
        }

        return null;
    }

    /// <summary>JSON body of <c>POST /api/lista-espera/baja</c>.</summary>
    public sealed record BajaRequest(string? Token);
}

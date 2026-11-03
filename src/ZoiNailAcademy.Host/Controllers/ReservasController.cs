using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.AbandonarReserva;
using ZoiNailAcademy.Application.Reservas.ObtenerReservaPublica;
using ZoiNailAcademy.Application.Reservas.ReservarConTarjeta;
using ZoiNailAcademy.Application.Reservas.ReservarManual;
using ZoiNailAcademy.Host.RateLimiting;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Public booking endpoints. The booking code is the only identifier the buyer sees.</summary>
[ApiController]
[Route("reservas")]
[AllowAnonymous]
public class ReservasController(ISender sender) : ControllerBase
{
    [HttpPost("tarjeta")]
    [EnableRateLimiting(RateLimitPolicies.Reservas)]
    [ProducesResponseType<ReservaTarjetaCreadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Tarjeta(ReservarConTarjetaCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Transfer or Bizum: holds the seat and returns the payment details and the WhatsApp link.</summary>
    [HttpPost("manual")]
    [EnableRateLimiting(RateLimitPolicies.Reservas)]
    [ProducesResponseType<ReservaPublicaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Manual(ReservarManualCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{codigo}")]
    [ProducesResponseType<ReservaPublicaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(string codigo, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObtenerReservaPublicaQuery(codigo), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(result.Value);
    }

    [HttpPost("{codigo}/abandonar")]
    [EnableRateLimiting(RateLimitPolicies.Reservas)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Abandonar(string codigo, CancellationToken cancellationToken)
    {
        await sender.Send(new AbandonarReservaCommand(codigo), cancellationToken);
        return NoContent();
    }
}

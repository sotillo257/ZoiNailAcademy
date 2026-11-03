using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.AmpliarReserva;
using ZoiNailAcademy.Application.Reservas.CancelarReserva;
using ZoiNailAcademy.Application.Reservas.ConfirmarReserva;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>The client's actions on a booking. Each one requires the <c>version</c> she read (409 <c>concurrencia</c>).</summary>
[ApiController]
[Route("admin/reservas")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminReservasController(ISender sender) : ControllerBase
{
    [HttpPost("{id:guid}/confirmar")]
    [ProducesResponseType<ReservaAccionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirmar(Guid id, ConfirmarReservaRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmarReservaCommand(id, request.Version, request.Nota), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType<ReservaAccionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancelarReservaRequest request, CancellationToken cancellationToken)
    {
        var command = new CancelarReservaCommand(id, request.Version, request.Motivo, request.ConfirmarPagada);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/ampliar")]
    [ProducesResponseType<ReservaAccionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ampliar(Guid id, AmpliarReservaRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AmpliarReservaCommand(id, request.Version, request.Horas), cancellationToken);
        return Ok(result.Value);
    }
}

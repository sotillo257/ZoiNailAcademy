using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.AmpliarReserva;
using ZoiNailAcademy.Application.Reservas.CancelarReserva;
using ZoiNailAcademy.Application.Reservas.ConfirmarReserva;
using ZoiNailAcademy.Application.Reservas.ListarPendientes;
using ZoiNailAcademy.Application.Reservas.MarcarRevisada;
using ZoiNailAcademy.Application.Reservas.ObtenerReservaAdmin;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>
/// The client's view of the bookings (pending list, search by code, detail) and her actions on one. Each action
/// requires the <c>version</c> she read (409 <c>concurrencia</c>).
/// </summary>
[ApiController]
[Route("admin/reservas")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminReservasController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Bookings that need review, valid pending ones and manual ones expired in the last 7 days.
    /// </summary>
    /// <param name="q">A booking code however it is typed (<c>CUR-7K4Q2M</c>, <c>cur 7k4q2m</c>, <c>7K4Q2M</c>), or name, email or phone.</param>
    /// <param name="metodo"><c>Stripe</c> or <c>Manual</c>.</param>
    [HttpGet("pendientes")]
    [ProducesResponseType<IReadOnlyList<ReservaAdminDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pendientes(
        [FromQuery] string? q, [FromQuery] MetodoPago? metodo, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarPendientesQuery(q, metodo), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReservaAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObtenerReservaAdminQuery(id), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Clears <c>requiereRevision</c> (e.g. after refunding a payment that arrived without a seat).</summary>
    [HttpPost("{id:guid}/marcar-revisada")]
    [ProducesResponseType<ReservaAccionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarcarRevisada(Guid id, MarcarRevisadaRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarcarRevisadaCommand(id, request.Version, request.Nota), cancellationToken);
        return Ok(result.Value);
    }

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

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Common.Csv;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Ediciones.BorrarEdicion;
using ZoiNailAcademy.Application.Ediciones.CrearEdicion;
using ZoiNailAcademy.Application.Ediciones.EditarEdicion;
using ZoiNailAcademy.Application.Ediciones.ListarEdicionesAdmin;
using ZoiNailAcademy.Application.Ediciones.ObtenerEdicionAdmin;
using ZoiNailAcademy.Application.Reservas.ExportarAsistentes;
using ZoiNailAcademy.Application.Reservas.ListarReservasEdicion;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Edition management for the client.</summary>
[ApiController]
[Route("admin/ediciones")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminEdicionesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EdicionAdminDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? cursoId, [FromQuery] bool incluirPasadas = false, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListarEdicionesAdminQuery(cursoId, incluirPasadas), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EdicionAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObtenerEdicionAdminQuery(id), cancellationToken);
        return Ok(result.Value);
    }

    /// <param name="incluir">Comma-separated: <c>pagadas</c>, <c>pendientes</c> (default), <c>expiradas</c>, <c>canceladas</c>.</param>
    [HttpGet("{id:guid}/reservas")]
    [ProducesResponseType<ReservasEdicionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reservas(Guid id, [FromQuery] string? incluir, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarReservasEdicionQuery(id, incluir), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Attendee list (paid bookings) as a CSV for Excel (UTF-8 with BOM, <c>;</c>, Madrid time).</summary>
    [HttpGet("{id:guid}/reservas.csv")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportarAsistentes(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportarAsistentesQuery(id), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(result.Value.Contenido, CsvBuilder.ContentType, result.Value.NombreArchivo);
    }

    [HttpPost]
    [ProducesResponseType<EdicionAdminDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear(CrearEdicionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<EdicionEditadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Editar(Guid id, EditarEdicionRequest request, CancellationToken cancellationToken)
    {
        var command = new EditarEdicionCommand(
            id, request.Inicio, request.Plazas, request.Estado, request.Notas, request.Version, request.ConfirmarCambioFecha);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Borrar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new BorrarEdicionCommand(id), cancellationToken);
        return NoContent();
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Csv;
using ZoiNailAcademy.Application.ListasEspera;
using ZoiNailAcademy.Application.ListasEspera.ExportarListaEspera;
using ZoiNailAcademy.Application.ListasEspera.ListarListaEspera;
using ZoiNailAcademy.Application.ListasEspera.MarcarContactado;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>The client's view of the waiting list: listing, "contacted" mark and CSV export.</summary>
[ApiController]
[Route("admin")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminListaEsperaController(ISender sender) : ControllerBase
{
    [HttpGet("lista-espera")]
    [ProducesResponseType<PagedResult<ListaEsperaAdminDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? cursoId, [FromQuery] bool? contactado, [FromQuery] string? q,
        [FromQuery] int page = ListarListaEsperaQuery.PrimeraPagina,
        [FromQuery] int pageSize = ListarListaEsperaQuery.PageSizePorDefecto,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListarListaEsperaQuery(cursoId, contactado, q, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPut("lista-espera/{id:guid}/contactado")]
    [ProducesResponseType<ListaEsperaAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarContactado(
        Guid id, MarcarContactadoRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarcarContactadoCommand(id, request.Contactado, request.Notas), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>CSV for Excel (UTF-8 with BOM, <c>;</c> separator, dates in Madrid time).</summary>
    [HttpGet("lista-espera.csv")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportarCsv([FromQuery] Guid? cursoId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExportarListaEsperaQuery(cursoId), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(result.Value.Contenido, CsvBuilder.ContentType, result.Value.NombreArchivo);
    }
}

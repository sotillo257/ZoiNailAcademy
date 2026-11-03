using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Cursos;
using ZoiNailAcademy.Application.Cursos.ListarCursos;
using ZoiNailAcademy.Application.Cursos.ObtenerCurso;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Ediciones.ListarEdicionesPublicas;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Public catalogue: active courses and their visible editions with free seats.</summary>
[ApiController]
[Route("cursos")]
[AllowAnonymous]
public class CursosController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CursoResumenDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarCursosQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType<CursoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ObtenerCursoQuery(slug), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Calendar of the course: never cached, free seats change with every booking.</summary>
    [HttpGet("{slug}/ediciones")]
    [ProducesResponseType<IReadOnlyList<EdicionPublicaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ediciones(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarEdicionesPublicasQuery(slug), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(result.Value);
    }
}

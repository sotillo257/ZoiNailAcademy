using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Application.Cursos;
using ZoiNailAcademy.Application.Cursos.CrearCurso;
using ZoiNailAcademy.Application.Cursos.DesactivarCurso;
using ZoiNailAcademy.Application.Cursos.EditarCurso;
using ZoiNailAcademy.Application.Cursos.ListarCursosAdmin;
using ZoiNailAcademy.Host.Authorization;

namespace ZoiNailAcademy.Host.Controllers;

/// <summary>Course selector for the backoffice; course management is SuperAdmin-only.</summary>
[ApiController]
[Route("admin/cursos")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class AdminCursosController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CursoOpcionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListarCursosAdminQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    [ProducesResponseType<CursoAdminDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearCursoCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Created($"/api/cursos/{result.Value.Slug}", result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    [ProducesResponseType<CursoEditadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Editar(Guid id, EditarCursoRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(id), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Deactivates the course (it is never deleted).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DesactivarCursoCommand(id), cancellationToken);
        return NoContent();
    }
}

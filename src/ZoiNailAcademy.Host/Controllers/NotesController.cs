using ZoiNailAcademy.Application.Notes.CreateNote;
using ZoiNailAcademy.Application.Notes.DeleteNote;
using ZoiNailAcademy.Application.Notes.GetNote;
using ZoiNailAcademy.Application.Notes.GetNotes;
using ZoiNailAcademy.Application.Notes.UpdateNote;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZoiNailAcademy.Host.Controllers;

[ApiController]
[Authorize]
[Route("notes")]
public class NotesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetNotesQuery(page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNote(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNoteQuery(id), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetNote), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateNote(Guid id, UpdateNoteCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { Id = id }, cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteNoteCommand(id), cancellationToken);
        return NoContent();
    }
}

using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Notes.GetNote;

public class GetNoteHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetNoteQuery, Result<NoteDto>>
{
    public async Task<Result<NoteDto>> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var note = await dbContext.Notes
            .Where(n => n.Id == request.Id && n.UserId == userId)
            .Select(n => new NoteDto(n.Id, n.Title, n.Content, n.CreatedAt, n.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        if (note is null)
        {
            throw new NotFoundException(nameof(Note), request.Id);
        }

        return Result.Success(note);
    }
}

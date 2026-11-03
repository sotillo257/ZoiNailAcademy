using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Notes.UpdateNote;

public class UpdateNoteHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<UpdateNoteCommand, Result<NoteDto>>
{
    public async Task<Result<NoteDto>> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var note = await dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);
        if (note is null)
        {
            throw new NotFoundException(nameof(Note), request.Id);
        }

        note.Update(request.Title, request.Content);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(NoteDto.FromEntity(note));
    }
}

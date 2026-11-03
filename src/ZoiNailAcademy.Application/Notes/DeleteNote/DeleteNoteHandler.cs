using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Notes.DeleteNote;

public class DeleteNoteHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<DeleteNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var note = await dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);
        if (note is null)
        {
            throw new NotFoundException(nameof(Note), request.Id);
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

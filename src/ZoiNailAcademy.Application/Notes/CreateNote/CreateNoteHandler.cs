using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.CreateNote;

public class CreateNoteHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<CreateNoteCommand, Result<NoteDto>>
{
    public async Task<Result<NoteDto>> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var note = Note.Create(userId, request.Title, request.Content);
        dbContext.Notes.Add(note);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(NoteDto.FromEntity(note));
    }
}

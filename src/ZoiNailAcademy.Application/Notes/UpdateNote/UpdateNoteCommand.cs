using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.UpdateNote;

public sealed record UpdateNoteCommand(Guid Id, string Title, string Content) : IRequest<Result<NoteDto>>;

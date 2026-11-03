using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.CreateNote;

public sealed record CreateNoteCommand(string Title, string Content) : IRequest<Result<NoteDto>>;

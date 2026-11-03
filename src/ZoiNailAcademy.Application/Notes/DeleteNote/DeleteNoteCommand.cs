using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.DeleteNote;

public sealed record DeleteNoteCommand(Guid Id) : IRequest<Result>;

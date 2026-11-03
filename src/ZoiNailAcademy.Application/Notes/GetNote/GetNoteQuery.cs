using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.GetNote;

public sealed record GetNoteQuery(Guid Id) : IRequest<Result<NoteDto>>;

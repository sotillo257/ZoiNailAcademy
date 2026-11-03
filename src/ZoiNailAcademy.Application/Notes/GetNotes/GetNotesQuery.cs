using ZoiNailAcademy.Application.Common;
using MediatR;

namespace ZoiNailAcademy.Application.Notes.GetNotes;

public sealed record GetNotesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<NoteDto>>>;

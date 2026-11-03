using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Notes.GetNotes;

public class GetNotesHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetNotesQuery, Result<PagedResult<NoteDto>>>
{
    public async Task<Result<PagedResult<NoteDto>>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var query = dbContext.Notes.Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenBy(n => n.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NoteDto(n.Id, n.Title, n.Content, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<NoteDto>(items, request.Page, request.PageSize, totalCount));
    }
}

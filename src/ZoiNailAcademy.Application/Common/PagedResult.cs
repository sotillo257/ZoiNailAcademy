namespace ZoiNailAcademy.Application.Common;

/// <summary>A page of a listing: JSON <c>{ items, total, page, pageSize }</c>.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

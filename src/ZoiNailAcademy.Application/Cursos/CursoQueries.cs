using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Enums;
using ValidationException = ZoiNailAcademy.Application.Common.Exceptions.ValidationException;

namespace ZoiNailAcademy.Application.Cursos;

internal static class CursoQueries
{
    public static async Task EnsureSlugLibreAsync(
        this IAppDbContext dbContext, string slug, Guid? cursoId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim();
        if (await dbContext.Cursos.AnyAsync(c => c.Slug == normalized && c.Id != cursoId, cancellationToken))
        {
            throw new ValidationException([new ValidationFailure(nameof(ICursoCampos.Slug), "Ya existe un curso con ese slug.")]);
        }
    }

    public static Task<bool> TieneEdicionesFuturasProgramadasAsync(
        this IAppDbContext dbContext, Guid cursoId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.Ediciones.AnyAsync(
            e => e.CursoId == cursoId && e.Estado == EstadoEdicion.Programada && e.Inicio > now, cancellationToken);
}

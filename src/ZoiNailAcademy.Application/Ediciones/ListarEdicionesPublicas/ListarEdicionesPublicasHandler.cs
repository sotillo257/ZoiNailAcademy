using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Ediciones.ListarEdicionesPublicas;

public class ListarEdicionesPublicasHandler(IAppDbContext dbContext, EdicionesPublicas edicionesPublicas)
    : IRequestHandler<ListarEdicionesPublicasQuery, Result<IReadOnlyList<EdicionPublicaDto>>>
{
    public async Task<Result<IReadOnlyList<EdicionPublicaDto>>> Handle(
        ListarEdicionesPublicasQuery request, CancellationToken cancellationToken)
    {
        var cursoId = await dbContext.Cursos
            .Where(c => c.Slug == request.Slug && c.Activo)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Curso", request.Slug);

        var ediciones = (await edicionesPublicas.PorCursoAsync([cursoId], cancellationToken)).GetValueOrDefault(cursoId) ?? [];
        return Result.Success(ediciones);
    }
}

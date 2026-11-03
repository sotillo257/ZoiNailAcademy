using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Cursos.ListarCursosAdmin;

public class ListarCursosAdminHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarCursosAdminQuery, Result<IReadOnlyList<CursoOpcionDto>>>
{
    public async Task<Result<IReadOnlyList<CursoOpcionDto>>> Handle(ListarCursosAdminQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<CursoOpcionDto> cursos = await dbContext.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .Select(c => new CursoOpcionDto(c.Id, c.Nombre, c.Slug, c.Activo))
            .ToListAsync(cancellationToken);

        return Result.Success(cursos);
    }
}

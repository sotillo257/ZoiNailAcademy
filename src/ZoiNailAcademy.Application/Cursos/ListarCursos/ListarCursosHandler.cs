using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Cursos.ListarCursos;

public class ListarCursosHandler(IAppDbContext dbContext, EdicionesPublicas edicionesPublicas)
    : IRequestHandler<ListarCursosQuery, Result<IReadOnlyList<CursoResumenDto>>>
{
    public async Task<Result<IReadOnlyList<CursoResumenDto>>> Handle(ListarCursosQuery request, CancellationToken cancellationToken)
    {
        var cursos = await dbContext.Cursos
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ToListAsync(cancellationToken);

        var ediciones = await edicionesPublicas.PorCursoAsync(cursos.Select(c => c.Id).ToList(), cancellationToken);

        IReadOnlyList<CursoResumenDto> dtos = cursos
            .Select(c =>
            {
                var delCurso = ediciones.GetValueOrDefault(c.Id) ?? [];
                return new CursoResumenDto(
                    c.Id, c.Slug, c.Nombre, c.DescripcionCorta, c.Precio, c.PrecioReserva, c.Imagen, c.Orden,
                    c.Ubicacion, c.DuracionTexto, ProximaEdicion.De(delCurso), delCurso);
            })
            .ToList();

        return Result.Success(dtos);
    }
}

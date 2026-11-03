using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Cursos.ObtenerCurso;

public class ObtenerCursoHandler(IAppDbContext dbContext, EdicionesPublicas edicionesPublicas)
    : IRequestHandler<ObtenerCursoQuery, Result<CursoDetalleDto>>
{
    public async Task<Result<CursoDetalleDto>> Handle(ObtenerCursoQuery request, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == request.Slug && c.Activo, cancellationToken)
            ?? throw new NotFoundException("Curso", request.Slug);

        var ediciones = (await edicionesPublicas.PorCursoAsync([curso.Id], cancellationToken))
            .GetValueOrDefault(curso.Id) ?? [];

        return Result.Success(new CursoDetalleDto(
            curso.Id, curso.Slug, curso.Nombre, curso.DescripcionCorta, curso.Precio, curso.PrecioReserva, curso.Imagen,
            curso.Orden, curso.Ubicacion, curso.DuracionTexto, ProximaEdicion.De(ediciones), curso.Descripcion,
            ediciones));
    }
}

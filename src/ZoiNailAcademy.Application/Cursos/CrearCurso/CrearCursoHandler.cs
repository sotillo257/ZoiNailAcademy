using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Cursos.CrearCurso;

public class CrearCursoHandler(IAppDbContext dbContext) : IRequestHandler<CrearCursoCommand, Result<CursoAdminDto>>
{
    public async Task<Result<CursoAdminDto>> Handle(CrearCursoCommand request, CancellationToken cancellationToken)
    {
        await dbContext.EnsureSlugLibreAsync(request.Slug, cursoId: null, cancellationToken);

        var curso = Curso.Create(
            request.Nombre, request.Slug, request.DescripcionCorta, request.Descripcion, request.Precio, request.Imagen,
            request.Ubicacion, request.DuracionTexto, request.Activo, request.Orden);

        dbContext.Cursos.Add(curso);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CursoAdminDto.From(curso));
    }
}

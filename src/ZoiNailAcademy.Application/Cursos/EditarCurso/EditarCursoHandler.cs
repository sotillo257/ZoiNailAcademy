using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Cursos.EditarCurso;

public class EditarCursoHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<EditarCursoCommand, Result<CursoEditadoDto>>
{
    public const string AvisoSlugCambiado =
        "Has cambiado el slug: las URLs antiguas del curso dejarán de funcionar (afecta al SEO).";

    public async Task<Result<CursoEditadoDto>> Handle(EditarCursoCommand request, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Curso", request.Id);

        await dbContext.EnsureSlugLibreAsync(request.Slug, curso.Id, cancellationToken);
        var tieneEdicionesFuturas = curso.Activo && !request.Activo
            && await dbContext.TieneEdicionesFuturasProgramadasAsync(curso.Id, timeProvider.GetUtcNow(), cancellationToken);

        var slugCambiado = curso.Actualizar(
            request.Nombre, request.Slug, request.DescripcionCorta, request.Descripcion, request.Precio, request.Imagen,
            request.Ubicacion, request.DuracionTexto, request.Activo, request.Orden, tieneEdicionesFuturas);

        await dbContext.SaveChangesAsync(cancellationToken);

        IReadOnlyList<string> avisos = slugCambiado ? [AvisoSlugCambiado] : [];
        return Result.Success(new CursoEditadoDto(CursoAdminDto.From(curso), avisos));
    }
}

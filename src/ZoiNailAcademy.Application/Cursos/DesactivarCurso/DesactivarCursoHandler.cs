using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Cursos.DesactivarCurso;

public class DesactivarCursoHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<DesactivarCursoCommand, Result>
{
    public async Task<Result> Handle(DesactivarCursoCommand request, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Curso", request.Id);

        var tieneEdicionesFuturas = await dbContext.TieneEdicionesFuturasProgramadasAsync(
            curso.Id, timeProvider.GetUtcNow(), cancellationToken);
        curso.Desactivar(tieneEdicionesFuturas);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

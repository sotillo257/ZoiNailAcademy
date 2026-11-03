using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Cursos.ObtenerCursoAdmin;

public class ObtenerCursoAdminHandler(IAppDbContext dbContext)
    : IRequestHandler<ObtenerCursoAdminQuery, Result<CursoAdminDto>>
{
    public async Task<Result<CursoAdminDto>> Handle(ObtenerCursoAdminQuery request, CancellationToken cancellationToken)
    {
        var curso = await dbContext.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Curso", request.Id);

        return Result.Success(CursoAdminDto.From(curso));
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Ediciones.ObtenerEdicionAdmin;

public class ObtenerEdicionAdminHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
    : IRequestHandler<ObtenerEdicionAdminQuery, Result<EdicionAdminDto>>
{
    public async Task<Result<EdicionAdminDto>> Handle(ObtenerEdicionAdminQuery request, CancellationToken cancellationToken)
    {
        var edicion = await dbContext.Ediciones
            .AsNoTracking()
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Edición", request.Id);

        var ocupaciones = await ocupacionReader.LeerAsync([edicion.Id], cancellationToken);
        return Result.Success(EdicionAdminDto.From(
            edicion, Ocupacion.De(ocupaciones, edicion.Id), timeProvider.GetUtcNow(), settings.CierreAntesDeInicio));
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Reservas.ObtenerReservaAdmin;

/// <summary>Detail of one booking in any state.</summary>
public class ObtenerReservaAdminHandler(IAppDbContext dbContext)
    : IRequestHandler<ObtenerReservaAdminQuery, Result<ReservaAdminDto>>
{
    public async Task<Result<ReservaAdminDto>> Handle(ObtenerReservaAdminQuery request, CancellationToken cancellationToken)
    {
        var fila = await dbContext.Reservas
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(ReservaAdminConsulta.Proyeccion)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Reserva", request.Id);

        return Result.Success(fila.ToDto());
    }
}

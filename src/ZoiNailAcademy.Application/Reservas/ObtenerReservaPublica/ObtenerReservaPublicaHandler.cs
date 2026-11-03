using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Reservas.ObtenerReservaPublica;

public class ObtenerReservaPublicaHandler(IAppDbContext dbContext)
    : IRequestHandler<ObtenerReservaPublicaQuery, Result<ReservaPublicaDto>>
{
    public async Task<Result<ReservaPublicaDto>> Handle(ObtenerReservaPublicaQuery request, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.Reservas
            .AsNoTracking()
            .Include(r => r.Edicion).ThenInclude(e => e.Curso)
            .FirstOrDefaultAsync(r => r.Codigo == request.Codigo, cancellationToken)
            ?? throw new NotFoundException("Reserva", request.Codigo);

        return Result.Success(ReservaPublicaDto.From(reserva));
    }
}

using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Reservas.MarcarRevisada;

/// <summary>The client handled a payment that arrived without a seat (e.g. refunded it) and clears the mark.</summary>
public class MarcarRevisadaHandler(IAppDbContext dbContext) : IRequestHandler<MarcarRevisadaCommand, Result<ReservaAccionDto>>
{
    public async Task<Result<ReservaAccionDto>> Handle(MarcarRevisadaCommand request, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.CargarAsync(request.Id, request.Version, cancellationToken);
        reserva.MarcarRevisada(request.Nota);
        await dbContext.GuardarAsync(cancellationToken);
        return Result.Success(new ReservaAccionDto(ReservaAdminDto.From(reserva), []));
    }
}

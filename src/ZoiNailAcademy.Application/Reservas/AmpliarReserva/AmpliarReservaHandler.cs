using MediatR;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Reservas.AmpliarReserva;

/// <summary>Gives a manual booking more time to pay; an expired one is reactivated if a seat is still free.</summary>
public class AmpliarReservaHandler(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
    : IRequestHandler<AmpliarReservaCommand, Result<ReservaAccionDto>>
{
    public const string AvisoRecortada = "La caducidad se ha recortado al cierre de reservas de la edición.";

    public async Task<Result<ReservaAccionDto>> Handle(AmpliarReservaCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
        var reserva = await dbContext.CargarAsync(request.Id, request.Version, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var cierre = reserva.Edicion.CierreReservas(settings.CierreAntesDeInicio);

        var hayPlaza = true;
        if (!reserva.OcupaPlaza(now))
        {
            await dbContext.BloquearEdicionAsync(reserva.EdicionId, cancellationToken);
            hayPlaza = await ocupacionReader.HayPlazaAsync(reserva.Edicion, cancellationToken);
        }

        reserva.Ampliar(now, request.Horas, cierre, hayPlaza);
        await dbContext.GuardarAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        IReadOnlyList<string> avisos = reserva.ExpiraEn == cierre ? [AvisoRecortada] : [];
        return Result.Success(new ReservaAccionDto(ReservaAdminDto.From(reserva), avisos));
    }
}

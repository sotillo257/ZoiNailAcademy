using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Application.Reservas.ConfirmarReserva;

/// <summary>The client received the transfer/Bizum receipt and confirms the payment.</summary>
public class ConfirmarReservaHandler(
    IAppDbContext dbContext,
    IOcupacionReader ocupacionReader,
    IPublisher publisher,
    TimeProvider timeProvider,
    ILogger<ConfirmarReservaHandler> logger)
    : IRequestHandler<ConfirmarReservaCommand, Result<ReservaAccionDto>>
{
    public async Task<Result<ReservaAccionDto>> Handle(ConfirmarReservaCommand request, CancellationToken cancellationToken)
    {
        bool pagada;
        ReservaAccionDto respuesta;

        await using (var transaction = await dbContext.BeginTransactionAsync(cancellationToken))
        {
            var reserva = await dbContext.CargarAsync(request.Id, request.Version, cancellationToken);
            var now = timeProvider.GetUtcNow();

            // A booking that no longer holds a seat needs a free one: lock the edition before counting.
            var hayPlaza = true;
            if (!reserva.OcupaPlaza(now))
            {
                await dbContext.BloquearEdicionAsync(reserva.EdicionId, cancellationToken);
                hayPlaza = await ocupacionReader.HayPlazaAsync(reserva.Edicion, cancellationToken);
            }

            pagada = reserva.ConfirmarPagoManual(now, hayPlaza, request.Nota);
            await dbContext.GuardarAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            respuesta = new ReservaAccionDto(ReservaAdminDto.From(reserva), []);
        }

        if (pagada)
        {
            await publisher.PublicarTrasCommitAsync(new ReservaPagadaEvent(request.Id), logger, cancellationToken);
        }

        return Result.Success(respuesta);
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.CancelarReserva;

public class CancelarReservaHandler(
    IAppDbContext dbContext,
    IStripeGateway stripe,
    IPublisher publisher,
    TimeProvider timeProvider,
    ILogger<CancelarReservaHandler> logger)
    : IRequestHandler<CancelarReservaCommand, Result<ReservaAccionDto>>
{
    public const string AvisoReembolso = "Recuerda reembolsar el pago";

    public async Task<Result<ReservaAccionDto>> Handle(CancelarReservaCommand request, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.CargarAsync(request.Id, request.Version, cancellationToken);
        var eraPagada = reserva.Estado == EstadoReserva.Pagada;

        // Close the card checkout first, so nobody can pay a booking we are cancelling.
        if (reserva is { Metodo: MetodoPago.Stripe, Estado: EstadoReserva.Pendiente, StripeSessionId: { } sessionId }
            && await stripe.ExpirarSesionAsync(sessionId, cancellationToken) == ResultadoExpiracionSesion.YaCompletada)
        {
            throw new BusinessRuleException(
                ErrorCodes.EstadoInvalido, "El pago con tarjeta acaba de completarse: recarga la reserva en unos segundos.");
        }

        var cancelada = reserva.CancelarDesdeBackoffice(timeProvider.GetUtcNow(), request.ConfirmarPagada, request.Motivo);
        await dbContext.GuardarAsync(cancellationToken);

        if (cancelada)
        {
            await publisher.PublicarTrasCommitAsync(new ReservaCanceladaEvent(reserva.Id), logger, cancellationToken);
        }

        IReadOnlyList<string> avisos = cancelada && eraPagada ? [AvisoReembolso] : [];
        return Result.Success(new ReservaAccionDto(ReservaAdminDto.From(reserva), avisos));
    }
}

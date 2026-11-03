using MediatR;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Reservas.AbandonarReserva;

public class AbandonarReservaHandler(IAppDbContext dbContext, IStripeGateway stripe, TimeProvider timeProvider)
    : IRequestHandler<AbandonarReservaCommand, Result>
{
    public async Task<Result> Handle(AbandonarReservaCommand request, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.Reservas.FirstOrDefaultAsync(r => r.Codigo == request.Codigo, cancellationToken)
            ?? throw new NotFoundException("Reserva", request.Codigo);

        if (reserva.Metodo != MetodoPago.Stripe || reserva.Estado != EstadoReserva.Pendiente)
        {
            return Result.Success();
        }

        // Expire the session first: once expired nobody can pay through it. If it was already paid,
        // leave the booking alone and let the "completed" webhook confirm it.
        if (reserva.StripeSessionId is { } sessionId
            && await stripe.ExpirarSesionAsync(sessionId, cancellationToken) == ResultadoExpiracionSesion.YaCompletada)
        {
            return Result.Success();
        }

        reserva.Cancelar(timeProvider.GetUtcNow());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The webhook changed the booking at the same time; its state wins.
        }

        return Result.Success();
    }
}

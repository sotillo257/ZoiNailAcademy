using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas.ProcesarWebhookStripe;

/// <summary>
/// Applies Stripe Checkout results. Each event id is recorded in <c>StripeEventos</c> in the same
/// transaction as its effect, so a redelivered event changes nothing; an unexpected error rolls both
/// back and the 500 makes Stripe retry.
/// </summary>
public class ProcesarWebhookStripeHandler(
    IAppDbContext dbContext,
    IStripeWebhookParser parser,
    IOcupacionReader ocupacionReader,
    IPublisher publisher,
    TimeProvider timeProvider,
    ILogger<ProcesarWebhookStripeHandler> logger)
    : IRequestHandler<ProcesarWebhookStripeCommand, Result>
{
    public async Task<Result> Handle(ProcesarWebhookStripeCommand request, CancellationToken cancellationToken)
    {
        var evento = parser.Parse(request.Json, request.Firma);
        if (evento.Tipo is not (StripeWebhookEvento.CheckoutCompletado or StripeWebhookEvento.CheckoutExpirado))
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        INotification? notificacion;

        await using (var transaction = await dbContext.BeginTransactionAsync(cancellationToken))
        {
            if (!await RegistrarEventoAsync(evento, now, cancellationToken))
            {
                return Result.Success();
            }

            notificacion = await AplicarAsync(evento, now, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await publisher.PublicarTrasCommitAsync(notificacion, logger, cancellationToken);
        return Result.Success();
    }

    /// <returns>False when the event was already processed.</returns>
    private async Task<bool> RegistrarEventoAsync(StripeWebhookEvento evento, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await dbContext.StripeEventos.AnyAsync(e => e.EventId == evento.Id, cancellationToken))
        {
            logger.LogInformation("Evento de Stripe {EventId} ya procesado; se ignora.", evento.Id);
            return false;
        }

        dbContext.StripeEventos.Add(StripeEvento.Create(evento.Id, evento.Tipo, now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The same event is being processed by a concurrent delivery, which committed its id first.
            logger.LogInformation("Evento de Stripe {EventId} procesado a la vez por otra entrega; se ignora.", evento.Id);
            return false;
        }
    }

    private async Task<INotification?> AplicarAsync(StripeWebhookEvento evento, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (evento.Tipo == StripeWebhookEvento.CheckoutCompletado && evento.PaymentStatus != "paid")
        {
            return null;
        }

        var reserva = await BuscarReservaAsync(evento, cancellationToken);
        if (reserva is null)
        {
            // Answer 200 anyway: an error would make Stripe retry an event we can never apply.
            logger.LogError(
                "Webhook {Tipo} de Stripe sin reserva: sesión {SessionId}, referencia {Referencia}.",
                evento.Tipo, evento.SessionId, evento.ClientReferenceId);
            return null;
        }

        return evento.Tipo == StripeWebhookEvento.CheckoutCompletado
            ? await CompletarAsync(reserva, evento.PaymentIntentId, now, cancellationToken)
            : Expirar(reserva, now);
    }

    private async Task<INotification?> CompletarAsync(
        Reserva reserva, string? paymentIntentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        switch (reserva.Estado)
        {
            case EstadoReserva.Pendiente:
                reserva.Pagar(now, paymentIntentId);
                return new ReservaPagadaEvent(reserva.Id);

            case EstadoReserva.Pagada:
                return null;

            default:
                // Paid after expiring or being cancelled: keep it only if a seat is still free.
                await dbContext.BloquearEdicionAsync(reserva.EdicionId, cancellationToken);
                var edicion = await dbContext.Ediciones.FirstAsync(e => e.Id == reserva.EdicionId, cancellationToken);
                var ocupacion = Ocupacion.De(
                    await ocupacionReader.LeerAsync([edicion.Id], cancellationToken), edicion.Id);

                if (PlazasCalculator.Libres(edicion.Plazas, ocupacion.Total) > 0)
                {
                    reserva.PagarTrasCaducidad(now, paymentIntentId);
                    return new ReservaPagadaEvent(reserva.Id);
                }

                reserva.RegistrarPagoSinPlaza(paymentIntentId);
                logger.LogWarning("Pago recibido sin plaza para la reserva {Codigo}: hay que reembolsarlo.", reserva.Codigo);
                return new PagoTrasCaducidadSinPlazaEvent(reserva.Id);
        }
    }

    private static INotification? Expirar(Reserva reserva, DateTimeOffset now)
    {
        if (reserva.Estado != EstadoReserva.Pendiente)
        {
            return null;
        }

        reserva.Expirar(now);
        return new ReservaExpiradaEvent(reserva.Id);
    }

    private async Task<Reserva?> BuscarReservaAsync(StripeWebhookEvento evento, CancellationToken cancellationToken)
    {
        if (evento.SessionId is { } sessionId
            && await dbContext.Reservas.FirstOrDefaultAsync(r => r.StripeSessionId == sessionId, cancellationToken) is { } porSesion)
        {
            return porSesion;
        }

        return Guid.TryParse(evento.ClientReferenceId, out var reservaId)
            ? await dbContext.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId, cancellationToken)
            : null;
    }
}

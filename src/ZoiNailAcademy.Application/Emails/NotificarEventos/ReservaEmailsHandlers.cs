using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Emails.NotificarEventos;

/// <summary>Loads the booking of an event with its edition and course (null when it no longer exists).</summary>
internal static class ReservaParaEmail
{
    public static async Task<Reserva?> CargarAsync(
        IAppDbContext dbContext, Guid reservaId, ILogger logger, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.Reservas.AsNoTracking()
            .Include(r => r.Edicion).ThenInclude(e => e.Curso)
            .FirstOrDefaultAsync(r => r.Id == reservaId, cancellationToken);
        if (reserva is null)
        {
            logger.LogWarning("No existe la reserva {ReservaId}; no se encolan sus emails.", reservaId);
        }

        return reserva;
    }
}

/// <summary>Paid booking (card or confirmed transfer): confirmation to the buyer and notice to the client.</summary>
public class ReservaPagadaEmailsHandler(
    IAppDbContext dbContext, BandejaSalida bandeja, IOcupacionReader ocupacionReader, ILogger<ReservaPagadaEmailsHandler> logger)
    : INotificationHandler<ReservaPagadaEvent>
{
    public async Task Handle(ReservaPagadaEvent notification, CancellationToken cancellationToken)
    {
        if (await ReservaParaEmail.CargarAsync(dbContext, notification.ReservaId, logger, cancellationToken) is not { } reserva)
        {
            return;
        }

        var marca = bandeja.Marca();
        var modelo = ReservaEmailModelo.From(reserva);
        await bandeja.EncolarParaClienteAsync(
            TiposEmail.ReservaPagadaComprador, reserva.Email, ReservaPagadaCompradorEmail.Crear(modelo, marca), reserva.Id,
            cancellationToken);

        var ocupacion = Ocupacion.De(await ocupacionReader.LeerAsync([reserva.EdicionId], cancellationToken), reserva.EdicionId);
        var libres = Math.Max(0, reserva.Edicion.Plazas - ocupacion.Total);
        await bandeja.EncolarParaClientaAsync(
            TiposEmail.ReservaPagadaClienta, ReservaPagadaClientaEmail.Crear(modelo, libres, marca), reserva.Id, cancellationToken);
    }
}

/// <summary>Manual booking: payment details to the buyer and a pending notice to the client.</summary>
public class ReservaManualCreadaEmailsHandler(
    IAppDbContext dbContext, BandejaSalida bandeja, ReservaPublicaDtoFactory dtoFactory,
    ILogger<ReservaManualCreadaEmailsHandler> logger)
    : INotificationHandler<ReservaManualCreadaEvent>
{
    public async Task Handle(ReservaManualCreadaEvent notification, CancellationToken cancellationToken)
    {
        if (await ReservaParaEmail.CargarAsync(dbContext, notification.ReservaId, logger, cancellationToken) is not { } reserva)
        {
            return;
        }

        var marca = bandeja.Marca();
        var modelo = ReservaEmailModelo.From(reserva);
        var datos = dtoFactory.DatosPago(reserva.Codigo);
        var pago = new DatosPagoEmail(
            datos.Titular, datos.Iban, datos.BizumTelefono,
            dtoFactory.WhatsApp(reserva.Codigo, modelo.Curso, reserva.Edicion.Inicio, reserva.Importe));

        await bandeja.EncolarParaClienteAsync(
            TiposEmail.ReservaManualComprador, reserva.Email, ReservaManualCompradorEmail.Crear(modelo, pago, marca), reserva.Id,
            cancellationToken);
        await bandeja.EncolarParaClientaAsync(
            TiposEmail.ReservaManualClienta, ReservaManualClientaEmail.Crear(modelo, marca), reserva.Id, cancellationToken);
    }
}

/// <summary>Expired booking: only manual buyers are told (a card buyer simply did not pay).</summary>
public class ReservaExpiradaEmailHandler(
    IAppDbContext dbContext, BandejaSalida bandeja, ILogger<ReservaExpiradaEmailHandler> logger)
    : INotificationHandler<ReservaExpiradaEvent>
{
    public async Task Handle(ReservaExpiradaEvent notification, CancellationToken cancellationToken)
    {
        if (await ReservaParaEmail.CargarAsync(dbContext, notification.ReservaId, logger, cancellationToken) is not { } reserva
            || reserva.Metodo != MetodoPago.Manual)
        {
            return;
        }

        await bandeja.EncolarParaClienteAsync(
            TiposEmail.ReservaExpiradaComprador, reserva.Email,
            ReservaExpiradaCompradorEmail.Crear(ReservaEmailModelo.From(reserva), bandeja.Marca()), reserva.Id,
            cancellationToken);
    }
}

/// <summary>A card payment arrived for an expired booking without a free seat: the client must refund it.</summary>
public class PagoSinPlazaEmailHandler(
    IAppDbContext dbContext, BandejaSalida bandeja, ILogger<PagoSinPlazaEmailHandler> logger)
    : INotificationHandler<PagoTrasCaducidadSinPlazaEvent>
{
    public async Task Handle(PagoTrasCaducidadSinPlazaEvent notification, CancellationToken cancellationToken)
    {
        if (await ReservaParaEmail.CargarAsync(dbContext, notification.ReservaId, logger, cancellationToken) is not { } reserva)
        {
            return;
        }

        await bandeja.EncolarParaClientaAsync(
            TiposEmail.PagoSinPlazaClienta, PagoSinPlazaClientaEmail.Crear(ReservaEmailModelo.From(reserva), bandeja.Marca()),
            reserva.Id, cancellationToken);
    }
}

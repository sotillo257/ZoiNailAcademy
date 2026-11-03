using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas.ReservarConTarjeta;

public class ReservarConTarjetaHandler(
    IAppDbContext dbContext,
    CrearReservaService crearReserva,
    IStripeGateway stripe,
    IReservasSettings settings,
    IAppSettings appSettings,
    TimeProvider timeProvider,
    ILogger<ReservarConTarjetaHandler> logger)
    : IRequestHandler<ReservarConTarjetaCommand, Result<ReservaTarjetaCreadaDto>>
{
    /// <summary>The booking outlives the Checkout session so Stripe's "expired" webhook arrives before the F04 job.</summary>
    public static readonly TimeSpan MargenCaducidad = TimeSpan.FromMinutes(5);

    public async Task<Result<ReservaTarjetaCreadaDto>> Handle(ReservarConTarjetaCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            // Honeypot: answer like a success so the bot learns nothing, and store nothing.
            logger.LogWarning("Honeypot relleno en una reserva con tarjeta; se ignora la petición.");
            var codigoFalso = CodigoReserva.Generar();
            return Result.Success(new ReservaTarjetaCreadaDto(codigoFalso, SuccessUrl(codigoFalso)));
        }

        var reserva = await crearReserva.CrearAsync(
            new NuevaReserva(
                request.EdicionId, request.Nombre, request.Email, request.Telefono, MetodoPago.Stripe,
                (_, ahora) => ahora + settings.StripeBloqueo + MargenCaducidad),
            cancellationToken);
        var now = reserva.ConsentimientoEn;

        CheckoutSesion sesion;
        try
        {
            sesion = await stripe.CrearSesionCheckoutAsync(SesionPara(reserva, now), cancellationToken);
        }
        catch (PaymentProviderException ex)
        {
            logger.LogError(ex, "No se pudo crear la sesión de Checkout de la reserva {Codigo}; se cancela.", reserva.Codigo);
            reserva.Cancelar(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        reserva.AsignarSesionStripe(sesion.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new ReservaTarjetaCreadaDto(reserva.Codigo, sesion.Url));
    }

    private CheckoutSesionRequest SesionPara(Reserva reserva, DateTimeOffset now)
    {
        var edicion = reserva.Edicion;
        var inicioLocal = TimeZoneInfo.ConvertTime(edicion.Inicio, appSettings.TimeZone);
        var concepto = $"{edicion.Curso.Nombre} — {inicioLocal.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}";

        return new CheckoutSesionRequest(
            reserva.Id,
            reserva.Codigo,
            concepto,
            (long)Math.Round(reserva.Importe * 100m, MidpointRounding.AwayFromZero),
            reserva.Email,
            now + settings.StripeBloqueo,
            SuccessUrl(reserva.Codigo),
            $"{appSettings.BaseUrl}/reservar/{edicion.Curso.Slug}?abandonada={reserva.Codigo}",
            $"checkout-{reserva.Id}");
    }

    private string SuccessUrl(string codigo) => $"{appSettings.BaseUrl}/reserva/{codigo}?origen=stripe";
}

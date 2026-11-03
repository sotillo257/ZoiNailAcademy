using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Events;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas.ReservarManual;

/// <summary>Booking paid by bank transfer or Bizum: the seat is held until the client confirms the payment.</summary>
public class ReservarManualHandler(
    IAppDbContext dbContext,
    CrearReservaService crearReserva,
    ReservaPublicaDtoFactory dtoFactory,
    IPublisher publisher,
    IReservasSettings settings,
    TimeProvider timeProvider,
    ILogger<ReservarManualHandler> logger)
    : IRequestHandler<ReservarManualCommand, Result<ReservaPublicaDto>>
{
    public async Task<Result<ReservaPublicaDto>> Handle(ReservarManualCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Website))
        {
            logger.LogWarning("Honeypot relleno en una reserva manual; se ignora la petición.");
            return Result.Success(await RespuestaFalsaAsync(request.EdicionId, cancellationToken));
        }

        var reserva = await crearReserva.CrearAsync(
            new NuevaReserva(
                request.EdicionId, request.Nombre, request.Email, request.Telefono, MetodoPago.Manual,
                (edicion, now) => Reserva.CalcularCaducidadManual(
                    now, settings.ManualCaducidad, edicion.CierreReservas(settings.CierreAntesDeInicio))),
            cancellationToken);

        await publisher.PublicarTrasCommitAsync(new ReservaManualCreadaEvent(reserva.Id), logger, cancellationToken);
        return Result.Success(dtoFactory.Crear(reserva));
    }

    // Looks like a real answer (same shape, real payment details) so a bot cannot tell; nothing is stored.
    private async Task<ReservaPublicaDto> RespuestaFalsaAsync(Guid edicionId, CancellationToken cancellationToken)
    {
        var codigo = CodigoReserva.Generar();
        var now = timeProvider.GetUtcNow();
        var edicion = await dbContext.Ediciones.AsNoTracking().Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == edicionId, cancellationToken);
        var curso = edicion?.Curso.Nombre ?? string.Empty;
        var inicio = edicion?.Inicio ?? now;
        var importe = edicion?.Curso.PrecioReserva ?? 0m;
        var precioCurso = edicion?.Curso.Precio ?? 0m;

        return new ReservaPublicaDto(
            codigo, EstadoReserva.Pendiente, MetodoPago.Manual, curso, inicio, importe, precioCurso,
            now + settings.ManualCaducidad,
            dtoFactory.DatosPago(codigo), dtoFactory.WhatsApp(codigo, curso, inicio, importe));
    }
}

using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common.Contacto;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;
using ValidationException = ZoiNailAcademy.Application.Common.Exceptions.ValidationException;

namespace ZoiNailAcademy.Application.Reservas;

public sealed record NuevaReserva(
    Guid EdicionId, string Nombre, string Email, string Telefono, MetodoPago Metodo, DateTimeOffset ExpiraEn);

/// <summary>
/// Booking core shared by card (F03) and manual (F04) payments. Inside one transaction it locks the
/// edition row, so concurrent bookings of the same edition are serialized and can never oversell.
/// </summary>
public class CrearReservaService(
    IAppDbContext dbContext, IOcupacionReader ocupacionReader, IReservasSettings settings, TimeProvider timeProvider)
{
    public const int MaxIntentosCodigo = 5;

    /// <returns>The committed <c>Pendiente</c> booking, with its edition and course loaded.</returns>
    public async Task<Reserva> CrearAsync(NuevaReserva nueva, CancellationToken cancellationToken)
    {
        if (!TelefonoNormalizer.TryNormalizar(nueva.Telefono, out var telefono))
        {
            throw new ValidationException([new ValidationFailure("Telefono", "El teléfono no es válido.")]);
        }

        var email = TelefonoNormalizer.NormalizarEmail(nueva.Email);
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);
        await dbContext.BloquearEdicionAsync(nueva.EdicionId, cancellationToken);

        var edicion = await dbContext.Ediciones
            .Include(e => e.Curso)
            .FirstOrDefaultAsync(e => e.Id == nueva.EdicionId, cancellationToken)
            ?? throw new NotFoundException("Edición", nueva.EdicionId);

        if (!edicion.EsReservable(edicion.Curso.Activo, now, settings.CierreAntesDeInicio))
        {
            throw new BusinessRuleException(ErrorCodes.EdicionNoReservable, "Esta fecha ya no admite reservas.");
        }

        var pendientesDelContacto = await dbContext.Reservas.CountAsync(
            r => r.Estado == EstadoReserva.Pendiente && r.ExpiraEn > now && (r.Email == email || r.Telefono == telefono),
            cancellationToken);
        if (pendientesDelContacto >= settings.MaxPendientesPorContacto)
        {
            throw new TooManyRequestsException(
                ErrorCodes.LimitePendientes,
                "Ya tienes reservas pendientes de pago. Complétalas o espera a que caduquen antes de reservar otra.");
        }

        var ocupacion = Ocupacion.De(await ocupacionReader.LeerAsync([edicion.Id], cancellationToken), edicion.Id);
        if (PlazasCalculator.Libres(edicion.Plazas, ocupacion.Total) == 0)
        {
            throw new BusinessRuleException(ErrorCodes.SinPlazas, "No quedan plazas libres en esta fecha.");
        }

        var reserva = Reserva.Create(
            edicion, await GenerarCodigoLibreAsync(cancellationToken), nueva.Metodo, nueva.Nombre, email, telefono,
            nueva.ExpiraEn, now);

        dbContext.Reservas.Add(reserva);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return reserva;
    }

    // The unique index is the final guarantee. The code is checked before inserting because a unique
    // violation would abort the PostgreSQL transaction and could not be retried inside it.
    private async Task<string> GenerarCodigoLibreAsync(CancellationToken cancellationToken)
    {
        for (var intento = 0; intento < MaxIntentosCodigo; intento++)
        {
            var codigo = CodigoReserva.Generar();
            if (!await dbContext.Reservas.AnyAsync(r => r.Codigo == codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException($"No se pudo generar un código de reserva libre en {MaxIntentosCodigo} intentos.");
    }
}

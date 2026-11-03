using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Services;

namespace ZoiNailAcademy.Application.Reservas;

/// <summary>Shared steps of the backoffice booking actions (confirm, cancel, extend).</summary>
internal static class AccionesReserva
{
    public const string ConcurrenciaMessage =
        "La reserva ha cambiado mientras la mirabas (por ejemplo, ha caducado). Recárgala y vuelve a intentarlo.";

    /// <summary>Loads the booking with its edition and course and checks the version the client read.</summary>
    public static async Task<Reserva> CargarAsync(
        this IAppDbContext dbContext, Guid id, string version, CancellationToken cancellationToken)
    {
        var reserva = await dbContext.Reservas
            .Include(r => r.Edicion).ThenInclude(e => e.Curso)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reserva", id);

        if (!uint.TryParse(version, out var leida) || leida != reserva.Version)
        {
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }

        return reserva;
    }

    /// <summary>Free seats of the edition. Call it after <see cref="IAppDbContext.BloquearEdicionAsync"/>.</summary>
    public static async Task<bool> HayPlazaAsync(
        this IOcupacionReader ocupacionReader, Edicion edicion, CancellationToken cancellationToken)
    {
        var ocupacion = Ocupacion.De(await ocupacionReader.LeerAsync([edicion.Id], cancellationToken), edicion.Id);
        return PlazasCalculator.Libres(edicion.Plazas, ocupacion.Total) > 0;
    }

    /// <summary>Saves; a concurrent change (xmin, e.g. the expiry job) becomes 409 <c>concurrencia</c>.</summary>
    public static async Task GuardarAsync(this IAppDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(ErrorCodes.Concurrencia, ConcurrenciaMessage);
        }
    }
}

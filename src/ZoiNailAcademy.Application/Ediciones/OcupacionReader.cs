using Microsoft.EntityFrameworkCore;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Ediciones;

/// <summary>
/// Occupancy from the <c>Reservas</c> table: one grouped query for all the requested editions, filtered
/// with <see cref="Reserva.OcupaPlazaEn"/> (paid, or pending and not expired).
/// </summary>
public class OcupacionReader(IAppDbContext dbContext, TimeProvider timeProvider) : IOcupacionReader
{
    public async Task<IReadOnlyDictionary<Guid, Ocupacion>> LeerAsync(
        IReadOnlyCollection<Guid> edicionIds, CancellationToken cancellationToken)
    {
        if (edicionIds.Count == 0)
        {
            return new Dictionary<Guid, Ocupacion>();
        }

        var now = timeProvider.GetUtcNow();
        var filas = await dbContext.Reservas
            .Where(r => edicionIds.Contains(r.EdicionId))
            .Where(Reserva.OcupaPlazaEn(now))
            .GroupBy(r => r.EdicionId)
            .Select(g => new
            {
                EdicionId = g.Key,
                Pagadas = g.Count(r => r.Estado == EstadoReserva.Pagada),
                Pendientes = g.Count(r => r.Estado == EstadoReserva.Pendiente)
            })
            .ToListAsync(cancellationToken);

        return filas.ToDictionary(f => f.EdicionId, f => new Ocupacion(f.Pagadas, f.Pendientes));
    }

    public Task<bool> TieneReservasAsync(Guid edicionId, CancellationToken cancellationToken) =>
        dbContext.Reservas.AnyAsync(r => r.EdicionId == edicionId, cancellationToken);
}

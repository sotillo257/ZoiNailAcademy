using ZoiNailAcademy.Application.Ediciones;

namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>
/// Reads the bookings that hold seats (see <c>PlazasCalculator.OcupaPlaza</c>). Listings call it once
/// for all their editions, so occupancy is computed in a single grouped query (no N+1).
/// </summary>
public interface IOcupacionReader
{
    /// <returns>Occupancy per edition; editions without bookings may be missing (use <see cref="Ocupacion.De"/>).</returns>
    Task<IReadOnlyDictionary<Guid, Ocupacion>> LeerAsync(IReadOnlyCollection<Guid> edicionIds, CancellationToken cancellationToken);

    /// <summary>Whether the edition has any booking, in any state.</summary>
    Task<bool> TieneReservasAsync(Guid edicionId, CancellationToken cancellationToken);
}

using System.Collections.Concurrent;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;

namespace IntegrationTests.TestSupport;

/// <summary>
/// Decorates the real <see cref="OcupacionReader"/> so a test can declare bookings for its own
/// editions until F03 adds the <c>Reserva</c> table. Edition ids are unique, so tests sharing the
/// factory do not interfere.
/// </summary>
public class TestOcupacionReader(OcupacionReader inner) : IOcupacionReader
{
    private static readonly ConcurrentDictionary<Guid, Ocupacion> Ocupaciones = new();
    private static readonly ConcurrentDictionary<Guid, bool> ConReservas = new();

    public static void Set(Guid edicionId, int pagadas, int pendientes = 0)
    {
        Ocupaciones[edicionId] = new Ocupacion(pagadas, pendientes);
        ConReservas[edicionId] = true;
    }

    /// <summary>Bookings that hold no seat (expired or cancelled): they still block deleting the edition.</summary>
    public static void SetSoloReservasSinPlaza(Guid edicionId) => ConReservas[edicionId] = true;

    public async Task<IReadOnlyDictionary<Guid, Ocupacion>> LeerAsync(
        IReadOnlyCollection<Guid> edicionIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Ocupacion>(await inner.LeerAsync(edicionIds, cancellationToken));
        foreach (var id in edicionIds)
        {
            if (Ocupaciones.TryGetValue(id, out var ocupacion))
            {
                result[id] = ocupacion;
            }
        }

        return result;
    }

    public async Task<bool> TieneReservasAsync(Guid edicionId, CancellationToken cancellationToken) =>
        ConReservas.ContainsKey(edicionId) || await inner.TieneReservasAsync(edicionId, cancellationToken);
}

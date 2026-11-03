using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Domain.Services;

/// <summary>
/// Single source of the free-seat rules, reused by the public calendar (F02), bookings (F03/F04)
/// and the backoffice (F07). Seats are always computed, never stored.
/// </summary>
public static class PlazasCalculator
{
    /// <summary>A booking holds a seat when it is paid, or pending and not expired yet.</summary>
    public static bool OcupaPlaza(EstadoReserva estado, DateTimeOffset? expiraEn, DateTimeOffset now) =>
        estado == EstadoReserva.Pagada
        || (estado == EstadoReserva.Pendiente && expiraEn is { } expira && expira > now);

    public static int Ocupadas(IEnumerable<(EstadoReserva Estado, DateTimeOffset? ExpiraEn)> reservas, DateTimeOffset now) =>
        reservas.Count(r => OcupaPlaza(r.Estado, r.ExpiraEn, now));

    public static int Libres(int plazas, int ocupadas) => Math.Max(0, plazas - ocupadas);

    public static EstadoEdicionPublico EstadoPublico(int libres, int umbralUltimasPlazas) =>
        libres <= 0 ? EstadoEdicionPublico.Completo
        : libres <= umbralUltimasPlazas ? EstadoEdicionPublico.UltimasPlazas
        : EstadoEdicionPublico.Disponible;
}

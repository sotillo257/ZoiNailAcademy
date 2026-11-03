namespace ZoiNailAcademy.Application.Ediciones;

/// <param name="Pagadas">Paid bookings.</param>
/// <param name="Pendientes">Pending bookings that have not expired yet.</param>
public sealed record Ocupacion(int Pagadas, int Pendientes)
{
    public static readonly Ocupacion Ninguna = new(0, 0);

    public int Total => Pagadas + Pendientes;

    public static Ocupacion De(IReadOnlyDictionary<Guid, Ocupacion> ocupaciones, Guid edicionId) =>
        ocupaciones.TryGetValue(edicionId, out var ocupacion) ? ocupacion : Ninguna;
}

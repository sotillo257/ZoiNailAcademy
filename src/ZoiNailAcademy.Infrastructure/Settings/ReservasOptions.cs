namespace ZoiNailAcademy.Infrastructure.Settings;

/// <summary>Section <c>Reservas</c>.</summary>
public class ReservasOptions
{
    public const string SectionName = "Reservas";

    /// <summary>Bookings close this many hours before an edition starts.</summary>
    public int CierreHorasAntes { get; set; } = 24;

    /// <summary>Free seats at or below this value are shown as "últimas plazas".</summary>
    public int UmbralUltimasPlazas { get; set; } = 3;
}

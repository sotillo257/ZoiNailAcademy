namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Booking settings (<c>Reservas:*</c>), implemented in Infrastructure.</summary>
public interface IReservasSettings
{
    /// <summary>Bookings close this long before an edition starts (<c>Reservas:CierreHorasAntes</c>).</summary>
    TimeSpan CierreAntesDeInicio { get; }

    /// <summary>Free seats at or below this value show "últimas plazas" (<c>Reservas:UmbralUltimasPlazas</c>).</summary>
    int UmbralUltimasPlazas { get; }
}

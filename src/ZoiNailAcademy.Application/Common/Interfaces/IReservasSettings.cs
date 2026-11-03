namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Booking settings (<c>Reservas:*</c>), implemented in Infrastructure.</summary>
public interface IReservasSettings
{
    /// <summary>Bookings close this long before an edition starts (<c>Reservas:CierreHorasAntes</c>).</summary>
    TimeSpan CierreAntesDeInicio { get; }

    /// <summary>Free seats at or below this value show "últimas plazas" (<c>Reservas:UmbralUltimasPlazas</c>).</summary>
    int UmbralUltimasPlazas { get; }

    /// <summary>Lifetime of the Stripe Checkout session (<c>Reservas:StripeMinutosBloqueo</c>, at least 30 min).</summary>
    TimeSpan StripeBloqueo { get; }

    /// <summary>Maximum valid pending bookings per email or phone (<c>Reservas:MaxPendientesPorContacto</c>).</summary>
    int MaxPendientesPorContacto { get; }

    /// <summary>Lifetime of a manual (transfer/Bizum) booking (<c>Reservas:ManualHorasCaducidad</c>).</summary>
    TimeSpan ManualCaducidad { get; }

    /// <summary>Interval of the expiry job (<c>Reservas:JobIntervaloMinutos</c>).</summary>
    TimeSpan JobIntervalo { get; }
}

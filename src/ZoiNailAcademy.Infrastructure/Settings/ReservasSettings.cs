using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Settings;

public class ReservasSettings(IOptions<ReservasOptions> options) : IReservasSettings
{
    public const int StripeMinutosMinimos = 30;

    public TimeSpan CierreAntesDeInicio => TimeSpan.FromHours(options.Value.CierreHorasAntes);

    public int UmbralUltimasPlazas => options.Value.UmbralUltimasPlazas;

    public TimeSpan StripeBloqueo => TimeSpan.FromMinutes(Math.Max(StripeMinutosMinimos, options.Value.StripeMinutosBloqueo));

    public int MaxPendientesPorContacto => options.Value.MaxPendientesPorContacto;
}

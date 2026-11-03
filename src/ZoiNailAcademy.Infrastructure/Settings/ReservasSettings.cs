using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Settings;

public class ReservasSettings(IOptions<ReservasOptions> options) : IReservasSettings
{
    public TimeSpan CierreAntesDeInicio => TimeSpan.FromHours(options.Value.CierreHorasAntes);

    public int UmbralUltimasPlazas => options.Value.UmbralUltimasPlazas;
}

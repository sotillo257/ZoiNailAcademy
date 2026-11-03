using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Settings;

public class AvisosSettings(IOptions<AvisosOptions> options) : IAvisosSettings
{
    public int LoteTamano => Math.Max(1, options.Value.LoteTamano);

    public TimeSpan PausaEntreLotes => TimeSpan.FromSeconds(Math.Max(0, options.Value.PausaEntreLotesSegundos));

    public TimeSpan JobIntervalo => TimeSpan.FromMinutes(Math.Max(1, options.Value.JobIntervaloMinutos));
}

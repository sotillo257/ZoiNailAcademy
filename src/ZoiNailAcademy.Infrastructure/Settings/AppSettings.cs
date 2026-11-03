using Microsoft.Extensions.Options;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Settings;

public class AppSettings(IOptions<AppOptions> options) : IAppSettings
{
    private readonly Lazy<TimeZoneInfo> _timeZone = new(() => TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone));

    public string BaseUrl => options.Value.BaseUrl.TrimEnd('/');

    public TimeZoneInfo TimeZone => _timeZone.Value;
}

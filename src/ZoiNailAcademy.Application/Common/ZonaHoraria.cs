namespace ZoiNailAcademy.Application.Common;

/// <summary>Calendar days in the configured time zone (<c>App:TimeZone</c>), not in UTC.</summary>
public static class ZonaHoraria
{
    /// <summary>The next midnight in <paramref name="zona"/> after <paramref name="now"/>: the end of "today".</summary>
    public static DateTimeOffset FinDelDia(DateTimeOffset now, TimeZoneInfo zona)
    {
        var manana = TimeZoneInfo.ConvertTime(now, zona).Date.AddDays(1);
        return new DateTimeOffset(manana, zona.GetUtcOffset(manana)).ToUniversalTime();
    }
}

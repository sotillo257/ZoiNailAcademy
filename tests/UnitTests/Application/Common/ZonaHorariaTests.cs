using FluentAssertions;
using ZoiNailAcademy.Application.Common;

namespace UnitTests.Application.Common;

public class ZonaHorariaTests
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    [Theory]
    [InlineData("2026-12-04T22:30:00Z", "2026-12-04T23:00:00Z")] // 23:30 in Madrid (winter, UTC+1)
    [InlineData("2026-12-04T23:30:00Z", "2026-12-05T23:00:00Z")] // 00:30 of the next day in Madrid
    [InlineData("2026-07-10T21:59:00Z", "2026-07-10T22:00:00Z")] // 23:59 in summer (UTC+2)
    [InlineData("2026-03-29T00:30:00Z", "2026-03-29T22:00:00Z")] // day of the change to summer time
    public void FinDelDia_ReturnsTheNextMidnightInTheTimeZone(string now, string esperado)
    {
        ZonaHoraria.FinDelDia(DateTimeOffset.Parse(now), Madrid).Should().Be(DateTimeOffset.Parse(esperado));
    }
}

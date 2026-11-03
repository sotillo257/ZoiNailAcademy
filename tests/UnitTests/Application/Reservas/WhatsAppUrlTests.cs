using FluentAssertions;
using ZoiNailAcademy.Application.Reservas;

namespace UnitTests.Application.Reservas;

public class WhatsAppUrlTests
{
    [Fact]
    public void Construir_EncodesAccentsEuroAndSpacesAndDecodesToTheMessage()
    {
        var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var inicioLocal = TimeZoneInfo.ConvertTime(new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero), madrid);

        var url = WhatsAppUrl.Construir("34600111222", "CUR-7K4Q2M", "Acrílico avanzado", inicioLocal, 149.90m);

        url.Should().StartWith("https://wa.me/34600111222?text=");
        var texto = url["https://wa.me/34600111222?text=".Length..];
        texto.Should().NotContain(" ").And.NotContain("í").And.NotContain("€");
        texto.Should().Contain("%20").And.Contain("%C3%AD").And.Contain("%E2%82%AC");
        Uri.UnescapeDataString(texto).Should().Be(
            "Hola, envío el comprobante de la reserva CUR-7K4Q2M (Acrílico avanzado, 05/12/2026 10:00). Importe: 149,90 €.");
    }

    [Fact]
    public void Construir_ThousandsUseSpanishSeparators()
    {
        var url = WhatsAppUrl.Construir("34600111222", "CUR-7K4Q2M", "Máster", DateTimeOffset.UnixEpoch, 1250m);

        Uri.UnescapeDataString(url).Should().EndWith("Importe: 1.250,00 €.");
    }
}

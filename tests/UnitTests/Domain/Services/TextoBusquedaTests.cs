using FluentAssertions;
using ZoiNailAcademy.Domain.Services;

namespace UnitTests.Domain.Services;

public class TextoBusquedaTests
{
    [Theory]
    [InlineData("José", "jose")]
    [InlineData("  MARÍA Núñez ", "maria nunez")]
    [InlineData("Àngels Güell", "angels guell")]
    [InlineData("lucia@mail.com", "lucia@mail.com")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalizar_RemovesAccentsAndCase(string? texto, string esperado)
    {
        TextoBusqueda.Normalizar(texto).Should().Be(esperado);
    }
}

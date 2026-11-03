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

    [Theory]
    [InlineData("612345678", "612345678")]
    [InlineData("612 34 56 78", "612345678")]
    [InlineData("+34 612-345.678", "34612345678")]
    [InlineData("(612) 345 678", "612345678")]
    public void DigitosTelefono_PhoneWithSeparators_ReturnsItsDigits(string texto, string esperado)
    {
        TextoBusqueda.DigitosTelefono(texto).Should().Be(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("+ -")]
    [InlineData("lucia 612")]
    [InlineData("7K4Q2M")]
    public void DigitosTelefono_TextThatIsNotAPhone_ReturnsNull(string? texto)
    {
        TextoBusqueda.DigitosTelefono(texto).Should().BeNull();
    }
}

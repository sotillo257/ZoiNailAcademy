using FluentAssertions;
using ZoiNailAcademy.Domain.Services;

namespace UnitTests.Domain.Services;

public class CodigoReservaTests
{
    [Fact]
    public void Generar_ProducesValidCodesWithoutAmbiguousCharacters()
    {
        var codigos = Enumerable.Range(0, 2000).Select(_ => CodigoReserva.Generar()).ToList();

        codigos.Should().OnlyContain(c => CodigoReserva.EsValido(c) && c.Length == CodigoReserva.Longitud);
        string.Concat(codigos.Select(c => c[4..])).Should().NotContainAny("0", "O", "1", "I", "L");
        codigos.Distinct().Count().Should().BeGreaterThan(1990, "codes are random");
    }

    [Theory]
    [InlineData("CUR-ABC234", true)]
    [InlineData("CUR-ABC23", false)]
    [InlineData("CUR-ABC2340", false)]
    [InlineData("CUR-ABCO23", false)]
    [InlineData("CUR-abc234", false)]
    [InlineData("XYZ-ABC234", false)]
    [InlineData(null, false)]
    public void EsValido_ChecksFormatAndAlphabet(string? codigo, bool expected)
    {
        CodigoReserva.EsValido(codigo).Should().Be(expected);
    }

    [Theory]
    [InlineData("CUR-7K4Q2M")]
    [InlineData("cur 7k4q2m")]
    [InlineData("cur-7k4q2m")]
    [InlineData("7K4Q2M")]
    [InlineData(" 7k4 q2m ")]
    [InlineData("CUR7K4Q2M")]
    public void TryNormalizar_CodeTypedInDifferentWays_ReturnsTheStoredCode(string texto)
    {
        CodigoReserva.TryNormalizar(texto, out var codigo).Should().BeTrue();
        codigo.Should().Be("CUR-7K4Q2M");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("7K4Q2")]
    [InlineData("7K4Q2MM")]
    [InlineData("CUR-7K4Q2")]
    [InlineData("7K4O2M")]
    [InlineData("lucia@mail.com")]
    [InlineData("612345678")]
    public void TryNormalizar_TextThatIsNotACode_ReturnsFalse(string? texto)
    {
        CodigoReserva.TryNormalizar(texto, out var codigo).Should().BeFalse();
        codigo.Should().BeEmpty();
    }
}

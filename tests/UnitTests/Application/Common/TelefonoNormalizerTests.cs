using FluentAssertions;
using ZoiNailAcademy.Application.Common.Contacto;

namespace UnitTests.Application.Common;

public class TelefonoNormalizerTests
{
    [Theory]
    [InlineData("612 34 56 78")]
    [InlineData("612345678")]
    [InlineData("+34612345678")]
    [InlineData("+34 612 34 56 78")]
    [InlineData("0034612345678")]
    public void TryNormalizar_SpanishFormats_ReturnsE164(string telefono)
    {
        TelefonoNormalizer.TryNormalizar(telefono, out var e164).Should().BeTrue();
        e164.Should().Be("+34612345678");
    }

    [Fact]
    public void TryNormalizar_ForeignNumberWithPrefix_KeepsItsCountry()
    {
        TelefonoNormalizer.TryNormalizar("+44 7911 123456", out var e164).Should().BeTrue();
        e164.Should().Be("+447911123456");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("612 34 56 7")]
    public void TryNormalizar_Invalid_ReturnsFalse(string? telefono)
    {
        TelefonoNormalizer.TryNormalizar(telefono, out _).Should().BeFalse();
    }

    [Fact]
    public void NormalizarEmail_TrimsAndLowerCases()
    {
        TelefonoNormalizer.NormalizarEmail("  Lucia@Mail.COM ").Should().Be("lucia@mail.com");
    }
}

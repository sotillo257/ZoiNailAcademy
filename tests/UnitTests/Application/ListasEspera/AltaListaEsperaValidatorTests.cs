using FluentAssertions;
using ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;

namespace UnitTests.Application.ListasEspera;

public class AltaListaEsperaValidatorTests
{
    private readonly AltaListaEsperaValidator _validator = new();

    private static AltaListaEsperaCommand Valid() =>
        new(Guid.NewGuid(), null, "Sábados de marzo", "Lucía Pérez", "lucia@mail.com", "612 34 56 78", true, null);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_HoneypotIsNotAValidationError()
    {
        _validator.Validate(Valid() with { Website = "http://spam" }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abc")]
    public void Validate_MissingOrInvalidPhone_FailsOnTelefono(string telefono)
    {
        _validator.Validate(Valid() with { Telefono = telefono }).Errors
            .Should().ContainSingle().Which.PropertyName.Should().Be("Telefono");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-email")]
    public void Validate_InvalidEmail_FailsOnEmail(string email)
    {
        _validator.Validate(Valid() with { Email = email }).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_PrivacyNotAccepted_FailsOnAceptaPrivacidad()
    {
        _validator.Validate(Valid() with { AceptaPrivacidad = false }).Errors
            .Should().ContainSingle().Which.PropertyName.Should().Be("AceptaPrivacidad");
    }

    [Fact]
    public void Validate_InvalidFields_FailOnEachField()
    {
        var result = _validator.Validate(
            new AltaListaEsperaCommand(Guid.Empty, null, new string('a', 201), "", "x", "1", false, null));

        result.Errors.Select(e => e.PropertyName).Distinct().Should().BeEquivalentTo(
            ["CursoId", "FechaPreferida", "Nombre", "Email", "Telefono", "AceptaPrivacidad"]);
    }
}

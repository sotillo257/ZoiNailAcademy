using FluentAssertions;
using ZoiNailAcademy.Application.Reservas.ReservarConTarjeta;

namespace UnitTests.Application.Reservas;

public class ReservarConTarjetaValidatorTests
{
    private readonly ReservarConTarjetaValidator _validator = new();

    private static ReservarConTarjetaCommand Valid() =>
        new(Guid.NewGuid(), "Lucía Pérez", "lucia@mail.com", "612 34 56 78", true, null);

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

    [Fact]
    public void Validate_InvalidFields_FailOnEachField()
    {
        var result = _validator.Validate(new ReservarConTarjetaCommand(Guid.Empty, "", "no-es-email", "123", false, null));

        result.Errors.Select(e => e.PropertyName).Distinct().Should().BeEquivalentTo(
            ["EdicionId", "Nombre", "Email", "Telefono", "AceptaCondiciones"]);
    }

    [Fact]
    public void Validate_TooLongName_FailsOnNombre()
    {
        _validator.Validate(Valid() with { Nombre = new string('a', 121) }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Nombre");
    }
}

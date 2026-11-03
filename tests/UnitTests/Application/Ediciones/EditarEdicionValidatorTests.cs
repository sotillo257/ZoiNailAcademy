using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Ediciones.EditarEdicion;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Ediciones;

public class EditarEdicionValidatorTests
{
    private readonly EditarEdicionValidator _validator = new();

    private static EditarEdicionCommand Valid() =>
        new(Guid.NewGuid(), CatalogoFactory.Now.AddDays(10), 12, EstadoEdicion.Programada, null, "1234", false);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_SeatsOutOfRange_FailsOnPlazas(int plazas)
    {
        _validator.Validate(Valid() with { Plazas = plazas }).Errors.Should().ContainSingle(e => e.PropertyName == "Plazas");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void Validate_InvalidVersion_FailsOnVersion(string version)
    {
        _validator.Validate(Valid() with { Version = version }).Errors.Should().ContainSingle(e => e.PropertyName == "Version");
    }

    [Fact]
    public void Validate_LongNotes_FailsOnNotas()
    {
        _validator.Validate(Valid() with { Notas = new string('x', 501) }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Notas");
    }

    [Fact]
    public void Validate_UndefinedState_FailsOnEstado()
    {
        _validator.Validate(Valid() with { Estado = (EstadoEdicion)99 }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Estado");
    }
}

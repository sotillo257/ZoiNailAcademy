using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Ediciones.CrearEdicion;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Ediciones;

public class CrearEdicionValidatorTests
{
    private readonly CrearEdicionValidator _validator = new(new FakeTimeProvider(CatalogoFactory.Now));

    private static CrearEdicionCommand Valid() =>
        new(Guid.NewGuid(), CatalogoFactory.Now.AddDays(10), 12, null, "nota");

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
        _validator.Validate(Valid() with { Estado = EstadoEdicion.Oculta }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validate_SeatsOutOfRange_FailsOnPlazas(int plazas)
    {
        _validator.Validate(Valid() with { Plazas = plazas }).Errors.Should().ContainSingle(e => e.PropertyName == "Plazas");
    }

    [Fact]
    public void Validate_PastStart_FailsOnInicio()
    {
        _validator.Validate(Valid() with { Inicio = CatalogoFactory.Now }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Inicio");
    }

    [Fact]
    public void Validate_CancelledState_FailsOnEstado()
    {
        _validator.Validate(Valid() with { Estado = EstadoEdicion.Cancelada }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Estado");
    }

    [Fact]
    public void Validate_LongNotes_FailsOnNotas()
    {
        _validator.Validate(Valid() with { Notas = new string('x', 501) }).Errors
            .Should().ContainSingle(e => e.PropertyName == "Notas");
    }

    [Fact]
    public void Validate_EmptyCourse_FailsOnCursoId()
    {
        _validator.Validate(Valid() with { CursoId = Guid.Empty }).Errors
            .Should().ContainSingle(e => e.PropertyName == "CursoId");
    }
}

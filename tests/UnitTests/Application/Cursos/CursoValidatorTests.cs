using FluentAssertions;
using ZoiNailAcademy.Application.Cursos.CrearCurso;
using ZoiNailAcademy.Application.Cursos.EditarCurso;

namespace UnitTests.Application.Cursos;

public class CursoValidatorTests
{
    private readonly CrearCursoValidator _crear = new();
    private readonly EditarCursoValidator _editar = new();

    private static CrearCursoCommand Valid() =>
        new("Acrílico", "acrilico", "Corta", "Larga", 149.99m, "/cursos/a.jpg", null, null, true, 1);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _crear.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyNameBadSlugAndZeroPrice_FailsOnEachField()
    {
        var result = _crear.Validate(Valid() with { Nombre = "", Slug = "Acrílico Pro", Precio = 0 });

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(["Nombre", "Slug", "Precio"]);
    }

    [Fact]
    public void Validate_TooLongFieldsAndNegativeOrder_FailOnEachField()
    {
        var result = _crear.Validate(Valid() with
        {
            Nombre = new string('a', 121),
            DescripcionCorta = new string('a', 301),
            Imagen = new string('a', 301),
            Ubicacion = new string('a', 301),
            DuracionTexto = new string('a', 101),
            Orden = -1,
            Precio = 1.234m
        });

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            ["Nombre", "DescripcionCorta", "Imagen", "Ubicacion", "DuracionTexto", "Orden", "Precio"]);
    }

    [Fact]
    public void Validate_EditCommand_UsesTheSameRules()
    {
        var command = new EditarCursoCommand(Guid.NewGuid(), "", "x--y", "c", "d", -5, "/i.jpg", null, null, true, 1);

        _editar.Validate(command).Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(["Nombre", "Slug", "Precio"]);
    }
}

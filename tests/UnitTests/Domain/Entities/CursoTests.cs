using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class CursoTests
{
    [Fact]
    public void Create_WithValidData_TrimsAndKeepsValues()
    {
        var curso = Curso.Create(
            "  Acrílico ", "acrilico-pro", " Corta ", "Larga", 149.50m, "/cursos/a.jpg", "  ", null, activo: false, orden: 2);

        curso.Id.Should().NotBeEmpty();
        curso.Nombre.Should().Be("Acrílico");
        curso.Slug.Should().Be("acrilico-pro");
        curso.DescripcionCorta.Should().Be("Corta");
        curso.Precio.Should().Be(149.50m);
        curso.Ubicacion.Should().BeNull();
        curso.Activo.Should().BeFalse();
        curso.Orden.Should().Be(2);
    }

    [Theory]
    [InlineData("Acrílico Pro")]
    [InlineData("acrilico--pro")]
    [InlineData("-acrilico")]
    [InlineData("ACRILICO")]
    [InlineData("")]
    public void Create_WithInvalidSlug_ThrowsDomainException(string slug)
    {
        var act = () => CatalogoFactory.Curso(slug: slug);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_ThrowsDomainException(decimal precio)
    {
        var act = () => CatalogoFactory.Curso(precio: precio);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithBlankName_ThrowsDomainException()
    {
        var act = () => Curso.Create(" ", "a", "c", "d", 1m, "/i.jpg", null, null, true, 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Actualizar_ChangingSlug_ReturnsTrue()
    {
        var curso = CatalogoFactory.Curso(slug: "acrilico");

        var slugCambiado = curso.Actualizar(
            "Acrílico", "acrilico-avanzado", "c", "d", 199m, "/i.jpg", null, null, true, 1, tieneEdicionesFuturasProgramadas: false);

        slugCambiado.Should().BeTrue();
        curso.Slug.Should().Be("acrilico-avanzado");
        curso.Precio.Should().Be(199m);
    }

    [Fact]
    public void Actualizar_KeepingSlug_ReturnsFalse()
    {
        var curso = CatalogoFactory.Curso(slug: "acrilico");

        curso.Actualizar("Otro", "acrilico", "c", "d", 1m, "/i.jpg", null, null, true, 1, false).Should().BeFalse();
    }

    [Fact]
    public void Actualizar_DeactivatingWithFutureScheduledEditions_ThrowsCursoConEdiciones()
    {
        var curso = CatalogoFactory.Curso();

        var act = () => curso.Actualizar("A", "acrilico", "c", "d", 1m, "/i.jpg", null, null, false, 1, true);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.CursoConEdiciones);
        curso.Activo.Should().BeTrue();
    }

    [Fact]
    public void Desactivar_WithFutureScheduledEditions_ThrowsAndStaysActive()
    {
        var curso = CatalogoFactory.Curso();

        var act = () => curso.Desactivar(tieneEdicionesFuturasProgramadas: true);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.CursoConEdiciones);
        curso.Activo.Should().BeTrue();
    }

    [Fact]
    public void Desactivar_WithoutFutureScheduledEditions_Deactivates()
    {
        var curso = CatalogoFactory.Curso();

        curso.Desactivar(tieneEdicionesFuturasProgramadas: false);

        curso.Activo.Should().BeFalse();
    }
}

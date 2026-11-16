using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

public class EdicionPreciosTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;
    private static readonly TimeSpan Cierre = TimeSpan.FromHours(24);

    [Fact]
    public void Create_WithoutPrices_CopiesTheCoursePrices()
    {
        var curso = CatalogoFactory.Curso(precio: 149m, precioReserva: 50m);

        var edicion = CatalogoFactory.Edicion(curso);

        (edicion.Precio, edicion.PrecioReserva).Should().Be((149m, 50m));
    }

    [Fact]
    public void Create_WithOwnPrices_UsesThemInsteadOfTheCourse()
    {
        var curso = CatalogoFactory.Curso(precio: 149m, precioReserva: 50m);

        var edicion = ZoiNailAcademy.Domain.Entities.Edicion.Create(
            curso, Now.AddDays(30), 10, EstadoEdicion.Programada, null, Now, precio: 200m, precioReserva: 80m);

        (edicion.Precio, edicion.PrecioReserva).Should().Be((200m, 80m));
        curso.Precio.Should().Be(149m, "changing the edition never changes the course");
    }

    [Fact]
    public void Create_OwnPriceBelowCourseDepositWithoutDeposit_CapsDepositAtThePrice()
    {
        var curso = CatalogoFactory.Curso(precio: 149m, precioReserva: 50m);

        var edicion = ZoiNailAcademy.Domain.Entities.Edicion.Create(
            curso, Now.AddDays(30), 10, EstadoEdicion.Programada, null, Now, precio: 40m);

        (edicion.Precio, edicion.PrecioReserva).Should().Be((40m, 40m));
    }

    [Fact]
    public void Create_DepositAbovePrice_ThrowsDomainException()
    {
        var act = () => ZoiNailAcademy.Domain.Entities.Edicion.Create(
            CatalogoFactory.Curso(), Now.AddDays(30), 10, EstadoEdicion.Programada, null, Now, precio: 100m, precioReserva: 120m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ZeroPrice_ThrowsDomainException()
    {
        var act = () => ZoiNailAcademy.Domain.Entities.Edicion.Create(
            CatalogoFactory.Curso(), Now.AddDays(30), 10, EstadoEdicion.Programada, null, Now, precio: 0m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Actualizar_WithPrices_ChangesThem()
    {
        var edicion = CatalogoFactory.Edicion(CatalogoFactory.Curso(precio: 149m, precioReserva: 50m));

        edicion.Actualizar(edicion.Inicio, edicion.Plazas, edicion.Estado, null, 0, false, Now, precio: 180m, precioReserva: 60m);

        (edicion.Precio, edicion.PrecioReserva).Should().Be((180m, 60m));
    }

    [Fact]
    public void Actualizar_WithoutPrices_KeepsThem()
    {
        var edicion = CatalogoFactory.Edicion(CatalogoFactory.Curso(precio: 149m, precioReserva: 50m));

        edicion.Actualizar(edicion.Inicio, 12, edicion.Estado, null, 0, false, Now);

        (edicion.Precio, edicion.PrecioReserva).Should().Be((149m, 50m));
    }

    [Fact]
    public void Actualizar_DepositAbovePrice_ThrowsDomainException()
    {
        var edicion = CatalogoFactory.Edicion();

        var act = () => edicion.Actualizar(edicion.Inicio, edicion.Plazas, edicion.Estado, null, 0, false, Now, precio: 100m, precioReserva: 101m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Actualizar_PricesOfAStartedEdition_ThrowsBusinessRule()
    {
        var edicion = CatalogoFactory.Edicion();

        var act = () => edicion.Actualizar(
            edicion.Inicio, edicion.Plazas, edicion.Estado, null, 0, false, edicion.Inicio.AddHours(1), precio: 200m);

        act.Should().Throw<ZoiNailAcademy.Domain.Common.Exceptions.BusinessRuleException>();
    }
}

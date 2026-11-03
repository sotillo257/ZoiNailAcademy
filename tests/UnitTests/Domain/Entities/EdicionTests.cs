using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

public class EdicionTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;
    private static readonly TimeSpan Cierre = TimeSpan.FromHours(24);

    [Fact]
    public void Create_WithValidData_SetsValues()
    {
        var curso = CatalogoFactory.Curso();
        var inicio = new DateTimeOffset(2026, 12, 5, 10, 0, 0, TimeSpan.FromHours(1));

        var edicion = Edicion.Create(curso, inicio, 12, EstadoEdicion.Programada, " nota ", Now);

        edicion.Id.Should().NotBeEmpty();
        edicion.CursoId.Should().Be(curso.Id);
        edicion.Inicio.Should().Be(inicio);
        edicion.Inicio.Offset.Should().Be(TimeSpan.Zero);
        edicion.Plazas.Should().Be(12);
        edicion.Estado.Should().Be(EstadoEdicion.Programada);
        edicion.Notas.Should().Be("nota");
    }

    [Fact]
    public void Create_ForInactiveCourse_ThrowsDomainException()
    {
        var act = () => CatalogoFactory.Edicion(CatalogoFactory.Curso(activo: false));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithPastStart_ThrowsDomainException()
    {
        var act = () => CatalogoFactory.Edicion(inicio: Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Create_WithSeatsOutOfRange_ThrowsDomainException(int plazas)
    {
        var act = () => CatalogoFactory.Edicion(plazas: plazas);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_AsCancelled_ThrowsDomainException()
    {
        var act = () => CatalogoFactory.Edicion(estado: EstadoEdicion.Cancelada);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EsReservable_ScheduledActiveAndAfterClosingWindow_IsTrue()
    {
        CatalogoFactory.Edicion(inicio: Now.AddHours(25)).EsReservable(true, Now, Cierre).Should().BeTrue();
    }

    [Theory]
    [InlineData(EstadoEdicion.Oculta, true, 25)]
    [InlineData(EstadoEdicion.Cancelada, true, 25)]
    [InlineData(EstadoEdicion.Programada, false, 25)]
    [InlineData(EstadoEdicion.Programada, true, 24)]
    [InlineData(EstadoEdicion.Programada, true, 2)]
    public void EsReservable_OtherwiseIsFalse(EstadoEdicion estado, bool cursoActivo, int horasHastaInicio)
    {
        var edicion = CatalogoFactory.Edicion(inicio: Now.AddHours(horasHastaInicio));
        if (estado != EstadoEdicion.Programada)
        {
            edicion.Actualizar(edicion.Inicio, edicion.Plazas, estado, null, 0, false, Now);
        }

        edicion.EsReservable(cursoActivo, Now, Cierre).Should().BeFalse();
    }

    [Fact]
    public void EsReservable_AlreadyStarted_IsFalse()
    {
        var edicion = CatalogoFactory.Edicion(inicio: Now.AddHours(1));

        edicion.EsReservable(true, Now.AddHours(2), Cierre).Should().BeFalse();
    }

    [Theory]
    [InlineData(EstadoEdicion.Programada, EstadoEdicion.Oculta, false)]
    [InlineData(EstadoEdicion.Oculta, EstadoEdicion.Programada, true)]
    [InlineData(EstadoEdicion.Programada, EstadoEdicion.Cancelada, false)]
    [InlineData(EstadoEdicion.Oculta, EstadoEdicion.Cancelada, false)]
    public void Actualizar_AllowedTransitions_Succeed(EstadoEdicion desde, EstadoEdicion hacia, bool publicada)
    {
        var edicion = CatalogoFactory.Edicion(estado: desde);

        var cambio = edicion.Actualizar(edicion.Inicio, edicion.Plazas, hacia, null, 0, false, Now);

        edicion.Estado.Should().Be(hacia);
        cambio.Publicada.Should().Be(publicada);
        cambio.Cancelada.Should().Be(hacia == EstadoEdicion.Cancelada);
        cambio.Avisos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EstadoEdicion.Programada)]
    [InlineData(EstadoEdicion.Oculta)]
    public void Actualizar_FromCancelled_ThrowsEstadoInvalido(EstadoEdicion hacia)
    {
        var edicion = Cancelada();

        var act = () => edicion.Actualizar(edicion.Inicio, edicion.Plazas, hacia, null, 0, false, Now);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        edicion.Estado.Should().Be(EstadoEdicion.Cancelada);
    }

    [Fact]
    public void Actualizar_CancelledEditionNotes_AreEditable()
    {
        var edicion = Cancelada();

        edicion.Actualizar(edicion.Inicio, edicion.Plazas, EstadoEdicion.Cancelada, "reembolsado", 0, false, Now);

        edicion.Notas.Should().Be("reembolsado");
    }

    [Fact]
    public void Actualizar_StartedEditionNotes_AreEditable()
    {
        var edicion = CatalogoFactory.Edicion(inicio: Now.AddHours(1));
        var later = Now.AddHours(2);

        edicion.Actualizar(edicion.Inicio, edicion.Plazas, edicion.Estado, "fue bien", 3, false, later);

        edicion.Notas.Should().Be("fue bien");
    }

    [Fact]
    public void Actualizar_StartedEditionSeats_ThrowsEstadoInvalido()
    {
        var edicion = CatalogoFactory.Edicion(inicio: Now.AddHours(1));

        var act = () => edicion.Actualizar(edicion.Inicio, 20, edicion.Estado, null, 0, false, Now.AddHours(2));

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        edicion.Plazas.Should().Be(10);
    }

    [Fact]
    public void Actualizar_SeatsBelowOccupied_ThrowsWithOcupadas()
    {
        var edicion = CatalogoFactory.Edicion(plazas: 10);

        var act = () => edicion.Actualizar(edicion.Inicio, 4, edicion.Estado, null, ocupadas: 5, false, Now);

        var ex = act.Should().Throw<BusinessRuleException>().Which;
        ex.Code.Should().Be(ErrorCodes.PlazasPorDebajoDeOcupadas);
        ex.Extensions["ocupadas"].Should().Be(5);
        edicion.Plazas.Should().Be(10);
    }

    [Fact]
    public void Actualizar_SeatsEqualToOccupied_Succeeds()
    {
        var edicion = CatalogoFactory.Edicion(plazas: 10);

        edicion.Actualizar(edicion.Inicio, 5, edicion.Estado, null, ocupadas: 5, false, Now);

        edicion.Plazas.Should().Be(5);
    }

    [Fact]
    public void Actualizar_DateChangeWithBookingsWithoutConfirmation_ThrowsWithReservas()
    {
        var edicion = CatalogoFactory.Edicion();
        var inicioOriginal = edicion.Inicio;

        var act = () => edicion.Actualizar(inicioOriginal.AddDays(7), 10, edicion.Estado, null, ocupadas: 3, false, Now);

        var ex = act.Should().Throw<BusinessRuleException>().Which;
        ex.Code.Should().Be(ErrorCodes.CambioFechaRequiereConfirmacion);
        ex.Extensions["reservas"].Should().Be(3);
        edicion.Inicio.Should().Be(inicioOriginal);
    }

    [Fact]
    public void Actualizar_DateChangeWithBookingsConfirmed_Succeeds()
    {
        var edicion = CatalogoFactory.Edicion();
        var nuevoInicio = edicion.Inicio.AddDays(7);

        edicion.Actualizar(nuevoInicio, 10, edicion.Estado, null, ocupadas: 3, confirmarCambioFecha: true, Now);

        edicion.Inicio.Should().Be(nuevoInicio);
    }

    [Fact]
    public void Actualizar_DateChangeWithoutBookings_DoesNotNeedConfirmation()
    {
        var edicion = CatalogoFactory.Edicion();
        var nuevoInicio = edicion.Inicio.AddDays(7);

        edicion.Actualizar(nuevoInicio, 10, edicion.Estado, null, ocupadas: 0, false, Now);

        edicion.Inicio.Should().Be(nuevoInicio);
    }

    [Fact]
    public void Actualizar_NewDateInThePast_ThrowsDomainException()
    {
        var edicion = CatalogoFactory.Edicion();

        var act = () => edicion.Actualizar(Now.AddHours(-1), 10, edicion.Estado, null, 0, false, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Actualizar_CancellingWithBookings_ReturnsWarning()
    {
        var edicion = CatalogoFactory.Edicion();

        var cambio = edicion.Actualizar(edicion.Inicio, 10, EstadoEdicion.Cancelada, null, ocupadas: 2, false, Now);

        cambio.Cancelada.Should().BeTrue();
        cambio.Avisos.Should().ContainSingle().Which.Should().Be("Tiene 2 inscritos: avisa y reembolsa manualmente");
    }

    private static Edicion Cancelada()
    {
        var edicion = CatalogoFactory.Edicion();
        edicion.Actualizar(edicion.Inicio, edicion.Plazas, EstadoEdicion.Cancelada, null, 0, false, Now);
        return edicion;
    }
}

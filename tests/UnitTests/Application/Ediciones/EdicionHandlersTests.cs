using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Ediciones.BorrarEdicion;
using ZoiNailAcademy.Application.Ediciones.CrearEdicion;
using ZoiNailAcademy.Application.Ediciones.EditarEdicion;
using ZoiNailAcademy.Application.Ediciones.ListarEdicionesAdmin;
using ZoiNailAcademy.Application.Ediciones.ObtenerEdicionAdmin;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Ediciones;

public class CrearEdicionHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly CrearEdicionHandler _handler;
    private readonly Curso _curso = CatalogoFactory.Curso();

    public CrearEdicionHandlerTests()
    {
        _ctx.SetupCursos(_curso);
        _handler = new CrearEdicionHandler(
            _ctx.DbContext.Object, _ctx.Publisher.Object, _ctx.Settings.Object, _ctx.TimeProvider,
            NullLogger<CrearEdicionHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithoutState_CreatesScheduledAndPublishesEvent()
    {
        var result = await _handler.Handle(
            new CrearEdicionCommand(_curso.Id, CatalogoFactory.Now.AddDays(10), 12, null, null), CancellationToken.None);

        var edicion = _ctx.Ediciones.Should().ContainSingle().Subject;
        edicion.Estado.Should().Be(EstadoEdicion.Programada);
        result.Value.Libres.Should().Be(12);
        result.Value.Curso.Should().Be(_curso.Nombre);
        result.Value.Reservable.Should().BeTrue();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _ctx.VerifyPublished(edicion.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_Hidden_DoesNotPublishEvent()
    {
        await _handler.Handle(
            new CrearEdicionCommand(_curso.Id, CatalogoFactory.Now.AddDays(10), 12, EstadoEdicion.Oculta, null),
            CancellationToken.None);

        _ctx.Ediciones.Should().ContainSingle().Which.Estado.Should().Be(EstadoEdicion.Oculta);
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_UnknownCourse_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(
            new CrearEdicionCommand(Guid.NewGuid(), CatalogoFactory.Now.AddDays(10), 12, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _ctx.VerifyNothingPublished();
    }
}

public class EditarEdicionHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly EditarEdicionHandler _handler;

    public EditarEdicionHandlerTests()
    {
        _handler = new EditarEdicionHandler(
            _ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Publisher.Object, _ctx.Settings.Object, _ctx.TimeProvider,
            NullLogger<EditarEdicionHandler>.Instance);
    }

    private static EditarEdicionCommand Command(Edicion e, EstadoEdicion? estado = null, int? plazas = null, string version = "0") =>
        new(e.Id, e.Inicio, plazas ?? e.Plazas, estado ?? e.Estado, e.Notas, version, false);

    [Fact]
    public async Task Handle_StaleVersion_ThrowsConcurrencia()
    {
        var edicion = CatalogoFactory.Edicion();
        _ctx.SetupEdiciones(edicion);

        var act = () => _handler.Handle(Command(edicion, plazas: 20, version: "7"), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be(ErrorCodes.Concurrencia);
        edicion.Plazas.Should().Be(10);
    }

    [Fact]
    public async Task Handle_HiddenToScheduled_PublishesEvent()
    {
        var edicion = CatalogoFactory.Edicion(estado: EstadoEdicion.Oculta);
        _ctx.SetupEdiciones(edicion);

        var result = await _handler.Handle(Command(edicion, EstadoEdicion.Programada), CancellationToken.None);

        result.Value.Edicion.Estado.Should().Be(EstadoEdicion.Programada);
        _ctx.VerifyPublished(edicion.Id, Times.Once());
    }

    [Theory]
    [InlineData(EstadoEdicion.Programada, EstadoEdicion.Oculta)]
    [InlineData(EstadoEdicion.Programada, EstadoEdicion.Programada)]
    [InlineData(EstadoEdicion.Programada, EstadoEdicion.Cancelada)]
    [InlineData(EstadoEdicion.Oculta, EstadoEdicion.Cancelada)]
    public async Task Handle_OtherTransitions_DoNotPublish(EstadoEdicion desde, EstadoEdicion hacia)
    {
        var edicion = CatalogoFactory.Edicion(estado: desde);
        _ctx.SetupEdiciones(edicion);

        await _handler.Handle(Command(edicion, hacia), CancellationToken.None);

        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_SeatsBelowOccupancyFromReader_ThrowsAndDoesNotSave()
    {
        var edicion = CatalogoFactory.Edicion();
        _ctx.SetupEdiciones(edicion);
        _ctx.Ocupaciones[edicion.Id] = new Ocupacion(3, 2);

        var act = () => _handler.Handle(Command(edicion, plazas: 4), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.PlazasPorDebajoDeOcupadas);
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancellingWithBookings_ReturnsWarningAndOccupancy()
    {
        var edicion = CatalogoFactory.Edicion();
        _ctx.SetupEdiciones(edicion);
        _ctx.Ocupaciones[edicion.Id] = new Ocupacion(1, 1);

        var result = await _handler.Handle(Command(edicion, EstadoEdicion.Cancelada), CancellationToken.None);

        result.Value.Avisos.Should().ContainSingle().Which.Should().Contain("Tiene 2 inscritos");
        result.Value.Edicion.Pagadas.Should().Be(1);
        result.Value.Edicion.Pendientes.Should().Be(1);
        result.Value.Edicion.Libres.Should().Be(8);
    }

    [Fact]
    public async Task Handle_UnknownEdition_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(Command(CatalogoFactory.Edicion()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class BorrarEdicionHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly BorrarEdicionHandler _handler;
    private readonly Edicion _edicion = CatalogoFactory.Edicion();

    public BorrarEdicionHandlerTests()
    {
        _ctx.SetupEdiciones(_edicion);
        _handler = new BorrarEdicionHandler(_ctx.DbContext.Object, _ctx.OcupacionReader.Object);
    }

    [Fact]
    public async Task Handle_WithoutBookings_RemovesEdition()
    {
        await _handler.Handle(new BorrarEdicionCommand(_edicion.Id), CancellationToken.None);

        _ctx.Ediciones.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBookings_ThrowsEdicionConReservas()
    {
        _ctx.OcupacionReader.Setup(x => x.TieneReservasAsync(_edicion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _handler.Handle(new BorrarEdicionCommand(_edicion.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be(ErrorCodes.EdicionConReservas);
        _ctx.Ediciones.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_UnknownEdition_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(new BorrarEdicionCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class ListarEdicionesAdminHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly ListarEdicionesAdminHandler _handler;
    private readonly Curso _curso = CatalogoFactory.Curso();
    private readonly Edicion _proxima;
    private readonly Edicion _lejana;
    private readonly Edicion _pasada;
    private readonly Edicion _otroCurso;

    public ListarEdicionesAdminHandlerTests()
    {
        _lejana = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(60));
        _proxima = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(5));
        _pasada = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(1));
        _otroCurso = CatalogoFactory.Edicion(CatalogoFactory.Curso("otro"), CatalogoFactory.Now.AddDays(3));
        _ctx.SetupEdiciones(_lejana, _pasada, _proxima, _otroCurso);
        _ctx.TimeProvider.Advance(TimeSpan.FromDays(2)); // _pasada already started
        _handler = new ListarEdicionesAdminHandler(
            _ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider);
    }

    [Fact]
    public async Task Handle_ByCourseWithoutPast_ReturnsUpcomingInOrder()
    {
        var result = await _handler.Handle(new ListarEdicionesAdminQuery(_curso.Id, false), CancellationToken.None);

        result.Value.Select(e => e.Id).Should().Equal(_proxima.Id, _lejana.Id);
    }

    [Fact]
    public async Task Handle_IncludingPast_PutsPastLast()
    {
        var result = await _handler.Handle(new ListarEdicionesAdminQuery(_curso.Id, true), CancellationToken.None);

        result.Value.Select(e => e.Id).Should().Equal(_proxima.Id, _lejana.Id, _pasada.Id);
        result.Value.Last().Reservable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutCourseFilter_ReturnsEveryCourseAndOccupancy()
    {
        _ctx.Ocupaciones[_proxima.Id] = new Ocupacion(4, 1);

        var result = await _handler.Handle(new ListarEdicionesAdminQuery(null, false), CancellationToken.None);

        result.Value.Select(e => e.Id).Should().Equal(_otroCurso.Id, _proxima.Id, _lejana.Id);
        var proxima = result.Value.Single(e => e.Id == _proxima.Id);
        (proxima.Pagadas, proxima.Pendientes, proxima.Libres).Should().Be((4, 1, 5));
        proxima.Version.Should().Be("0");
    }
}

public class ObtenerEdicionAdminHandlerTests
{
    [Fact]
    public async Task Handle_UnknownEdition_ThrowsNotFoundException()
    {
        var ctx = new CatalogoTestContext();
        var handler = new ObtenerEdicionAdminHandler(
            ctx.DbContext.Object, ctx.OcupacionReader.Object, ctx.Settings.Object, ctx.TimeProvider);

        var act = () => handler.Handle(new ObtenerEdicionAdminQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

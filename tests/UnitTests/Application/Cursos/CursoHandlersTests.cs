using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Cursos.CrearCurso;
using ZoiNailAcademy.Application.Cursos.DesactivarCurso;
using ZoiNailAcademy.Application.Cursos.EditarCurso;
using ZoiNailAcademy.Application.Cursos.ListarCursos;
using ZoiNailAcademy.Application.Cursos.ObtenerCurso;
using ZoiNailAcademy.Application.Cursos.ObtenerCursoAdmin;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Cursos;

public class CrearCursoHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private static CrearCursoCommand Command(string slug) =>
        new("Gel", slug, "Corta", "Larga", 99m, "/cursos/gel.jpg", null, null, true, 2);

    [Fact]
    public async Task Handle_NewSlug_CreatesCourse()
    {
        var result = await new CrearCursoHandler(_ctx.DbContext.Object).Handle(Command("gel"), CancellationToken.None);

        _ctx.Cursos.Should().ContainSingle().Which.Slug.Should().Be("gel");
        result.Value.Precio.Should().Be(99m);
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsValidationExceptionOnSlug()
    {
        _ctx.SetupCursos(CatalogoFactory.Curso("gel"));

        var act = () => new CrearCursoHandler(_ctx.DbContext.Object).Handle(Command("gel"), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("Slug");
    }
}

public class ObtenerCursoAdminHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task Handle_InactiveCourse_ReturnsEveryField()
    {
        var curso = CatalogoFactory.Curso("acrilico", activo: false, orden: 3, precio: 120m);
        _ctx.SetupCursos(curso, CatalogoFactory.Curso("gel"));

        var dto = (await new ObtenerCursoAdminHandler(_ctx.DbContext.Object)
            .Handle(new ObtenerCursoAdminQuery(curso.Id), CancellationToken.None)).Value;

        dto.Should().Be(new ZoiNailAcademy.Application.Cursos.CursoAdminDto(
            curso.Id, "Acrílico", "acrilico", "Curso de uñas acrílicas", "# Temario", 120m, "/cursos/acrilico.jpg",
            "Madrid", "2 días", false, 3));
    }

    [Fact]
    public async Task Handle_UnknownCourse_ThrowsNotFoundException()
    {
        var act = () => new ObtenerCursoAdminHandler(_ctx.DbContext.Object)
            .Handle(new ObtenerCursoAdminQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class EditarCursoHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Curso _curso = CatalogoFactory.Curso("acrilico");
    private readonly EditarCursoHandler _handler;

    public EditarCursoHandlerTests()
    {
        _ctx.SetupCursos(_curso, CatalogoFactory.Curso("gel"));
        _handler = new EditarCursoHandler(_ctx.DbContext.Object, _ctx.TimeProvider);
    }

    private EditarCursoCommand Command(string slug, bool activo = true) =>
        new(_curso.Id, "Acrílico", slug, "Corta", "Larga", 149m, "/cursos/a.jpg", null, null, activo, 1);

    [Fact]
    public async Task Handle_ChangingSlug_WarnsAboutUrls()
    {
        var result = await _handler.Handle(Command("acrilico-pro"), CancellationToken.None);

        result.Value.Curso.Slug.Should().Be("acrilico-pro");
        result.Value.Avisos.Should().ContainSingle().Which.Should().Be(EditarCursoHandler.AvisoSlugCambiado);
    }

    [Fact]
    public async Task Handle_KeepingSlug_HasNoWarnings()
    {
        var result = await _handler.Handle(Command("acrilico"), CancellationToken.None);

        result.Value.Avisos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SlugOfAnotherCourse_ThrowsValidationException()
    {
        var act = () => _handler.Handle(Command("gel"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_DeactivatingWithFutureScheduledEdition_ThrowsCursoConEdiciones()
    {
        _ctx.SetupEdiciones(CatalogoFactory.Edicion(_curso));

        var act = () => _handler.Handle(Command("acrilico", activo: false), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.CursoConEdiciones);
    }

    [Fact]
    public async Task Handle_UnknownCourse_ThrowsNotFoundException()
    {
        var act = () => _handler.Handle(Command("x") with { Id = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DesactivarCursoHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Curso _curso = CatalogoFactory.Curso();
    private readonly DesactivarCursoHandler _handler;

    public DesactivarCursoHandlerTests()
    {
        _ctx.SetupCursos(_curso);
        _handler = new DesactivarCursoHandler(_ctx.DbContext.Object, _ctx.TimeProvider);
    }

    [Fact]
    public async Task Handle_WithOnlyHiddenOrPastEditions_Deactivates()
    {
        var oculta = CatalogoFactory.Edicion(_curso, estado: EstadoEdicion.Oculta);
        var pasada = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(1));
        _ctx.SetupEdiciones(oculta, pasada);
        _ctx.TimeProvider.Advance(TimeSpan.FromDays(2));

        await _handler.Handle(new DesactivarCursoCommand(_curso.Id), CancellationToken.None);

        _curso.Activo.Should().BeFalse();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFutureScheduledEdition_ThrowsCursoConEdiciones()
    {
        _ctx.SetupEdiciones(CatalogoFactory.Edicion(_curso));

        var act = () => _handler.Handle(new DesactivarCursoCommand(_curso.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.CursoConEdiciones);
        _curso.Activo.Should().BeTrue();
    }
}

public class ListarCursosHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task Handle_ReturnsActiveCoursesByOrderWithNextEditionPreferringFreeSeats()
    {
        var segundo = CatalogoFactory.Curso("segundo", orden: 2);
        var primero = CatalogoFactory.Curso("primero", orden: 1);
        var inactivo = CatalogoFactory.Curso("inactivo", activo: false, orden: 0);
        var completa = CatalogoFactory.Edicion(primero, CatalogoFactory.Now.AddDays(5), plazas: 2);
        var conPlazas = CatalogoFactory.Edicion(primero, CatalogoFactory.Now.AddDays(9));
        var soloCompleta = CatalogoFactory.Edicion(segundo, CatalogoFactory.Now.AddDays(5), plazas: 2);
        var oculta = CatalogoFactory.Edicion(segundo, CatalogoFactory.Now.AddDays(2), estado: EstadoEdicion.Oculta);
        _ctx.SetupCursos(segundo, inactivo, primero);
        _ctx.SetupEdiciones(conPlazas, completa, soloCompleta, oculta);
        _ctx.Ocupaciones[completa.Id] = new Ocupacion(2, 0);
        _ctx.Ocupaciones[soloCompleta.Id] = new Ocupacion(1, 1);

        var result = await new ListarCursosHandler(_ctx.DbContext.Object, _ctx.CreateEdicionesPublicas())
            .Handle(new ListarCursosQuery(), CancellationToken.None);

        result.Value.Select(c => c.Slug).Should().Equal("primero", "segundo");
        result.Value[0].ProximaEdicion!.Id.Should().Be(conPlazas.Id);
        result.Value[1].ProximaEdicion!.Id.Should().Be(soloCompleta.Id);
        result.Value[1].ProximaEdicion!.Estado.Should().Be(EstadoEdicionPublico.Completo);
    }

    [Fact]
    public async Task Handle_CourseWithoutVisibleEditions_HasNoNextEdition()
    {
        var curso = CatalogoFactory.Curso();
        _ctx.SetupCursos(curso);
        _ctx.SetupEdiciones(CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddHours(23)));

        var result = await new ListarCursosHandler(_ctx.DbContext.Object, _ctx.CreateEdicionesPublicas())
            .Handle(new ListarCursosQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.ProximaEdicion.Should().BeNull();
    }
}

public class ObtenerCursoHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task Handle_InactiveCourse_ThrowsNotFoundException()
    {
        _ctx.SetupCursos(CatalogoFactory.Curso("apagado", activo: false));

        var act = () => new ObtenerCursoHandler(_ctx.DbContext.Object, _ctx.CreateEdicionesPublicas())
            .Handle(new ObtenerCursoQuery("apagado"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ActiveCourse_ReturnsVisibleEditionsByStart()
    {
        var curso = CatalogoFactory.Curso();
        var tarde = CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddDays(20));
        var pronto = CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddDays(3));
        var cancelada = CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddDays(4));
        cancelada.Actualizar(cancelada.Inicio, cancelada.Plazas, EstadoEdicion.Cancelada, null, 0, false, CatalogoFactory.Now);
        _ctx.SetupCursos(curso);
        _ctx.SetupEdiciones(tarde, cancelada, pronto);

        var result = await new ObtenerCursoHandler(_ctx.DbContext.Object, _ctx.CreateEdicionesPublicas())
            .Handle(new ObtenerCursoQuery(curso.Slug), CancellationToken.None);

        result.Value.Ediciones.Select(e => e.Id).Should().Equal(pronto.Id, tarde.Id);
        result.Value.Descripcion.Should().Be(curso.Descripcion);
    }
}

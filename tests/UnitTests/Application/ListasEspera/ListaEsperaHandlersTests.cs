using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.ListasEspera;
using ZoiNailAcademy.Application.ListasEspera.AltaListaEspera;
using ZoiNailAcademy.Application.ListasEspera.ExportarListaEspera;
using ZoiNailAcademy.Application.ListasEspera.ListarListaEspera;
using ZoiNailAcademy.Application.ListasEspera.MarcarContactado;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Application.ListasEspera;

public class AltaListaEsperaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Curso _curso = CatalogoFactory.Curso();

    public AltaListaEsperaHandlerTests() => _ctx.SetupCursos(_curso);

    private AltaListaEsperaHandler Handler() =>
        new(_ctx.DbContext.Object, _ctx.Publisher.Object, _ctx.TimeProvider, NullLogger<AltaListaEsperaHandler>.Instance);

    private AltaListaEsperaCommand Command(
        string email = "Lucia@Mail.com", Guid? cursoId = null, Guid? edicionId = null, string? website = null) =>
        new(cursoId ?? _curso.Id, edicionId, "Sábados", "Lucía", email, "612 34 56 78", true, website);

    [Fact]
    public async Task Handle_NewPerson_StoresTheBackendConsentAndPublishesTheEvent()
    {
        await Handler().Handle(Command(), CancellationToken.None);

        var entrada = _ctx.ListaEspera.Should().ContainSingle().Subject;
        entrada.Email.Should().Be("lucia@mail.com");
        entrada.Telefono.Should().Be("+34612345678");
        entrada.TextoConsentimiento.Should().Be(ConsentimientoListaEspera.V1);
        entrada.TextoConsentimiento.Should().Contain("email").And.Contain("nuevas fechas");
        entrada.ConsentimientoEn.Should().Be(CatalogoFactory.Now);
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        _ctx.VerifyPublished<ListaEsperaAltaEvent>(e => e.ListaEsperaId == entrada.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_SamePersonAndCourse_UpdatesTheEntryAndPublishesAgain()
    {
        var existente = ListaEspera.Create(_curso, null, null, "Antes", "lucia@mail.com", "+34698765432", "Viejo", CatalogoFactory.Now.AddDays(-5));
        existente.MarcarContactado(true, CatalogoFactory.Now.AddDays(-1));
        _ctx.SetupListaEspera(existente);

        await Handler().Handle(Command(), CancellationToken.None);

        _ctx.ListaEspera.Should().ContainSingle().Which.Should().BeSameAs(existente);
        existente.Nombre.Should().Be("Lucía");
        existente.Telefono.Should().Be("+34612345678");
        existente.Contactado.Should().BeFalse();
        existente.TextoConsentimiento.Should().Be(ConsentimientoListaEspera.V1);
        _ctx.VerifyPublished<ListaEsperaAltaEvent>(e => e.ListaEsperaId == existente.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_InactiveOrUnknownCourse_ThrowsNotFound()
    {
        var inactivo = CatalogoFactory.Curso("inactivo", activo: false);
        _ctx.SetupCursos(_curso, inactivo);

        var inactiveAct = () => Handler().Handle(Command(cursoId: inactivo.Id), CancellationToken.None);
        var unknownAct = () => Handler().Handle(Command(cursoId: Guid.NewGuid()), CancellationToken.None);

        await inactiveAct.Should().ThrowAsync<NotFoundException>();
        await unknownAct.Should().ThrowAsync<NotFoundException>();
        _ctx.ListaEspera.Should().BeEmpty();
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_EditionOfAnotherCourse_ThrowsValidationOnEdicionPreferidaId()
    {
        var otraEdicion = CatalogoFactory.Edicion(CatalogoFactory.Curso("otro"));
        _ctx.SetupEdiciones(otraEdicion);

        var act = () => Handler().Handle(Command(edicionId: otraEdicion.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("EdicionPreferidaId");
        _ctx.ListaEspera.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EditionOfTheCourse_IsStoredAsPreferred()
    {
        var edicion = CatalogoFactory.Edicion(_curso);
        _ctx.SetupEdiciones(edicion);

        await Handler().Handle(Command(edicionId: edicion.Id), CancellationToken.None);

        _ctx.ListaEspera.Should().ContainSingle().Which.EdicionPreferidaId.Should().Be(edicion.Id);
    }

    [Fact]
    public async Task Handle_Honeypot_StoresNothingAndPublishesNothing()
    {
        var result = await Handler().Handle(Command(website: "http://spam"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _ctx.ListaEspera.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        _ctx.VerifyNothingPublished();
    }
}

public class ListaEsperaAdminHandlersTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Curso _curso = CatalogoFactory.Curso();

    public ListaEsperaAdminHandlersTests() => _ctx.SetupCursos(_curso);

    private ListaEspera Entrada(string nombre, DateTimeOffset creadoEn, bool contactado = false, Curso? curso = null)
    {
        var entrada = ListaEspera.Create(
            curso ?? _curso, null, null, nombre, $"{Guid.NewGuid():N}@mail.com", "+34612345678", ConsentimientoListaEspera.V1,
            creadoEn);
        entrada.CreatedAt = creadoEn;
        entrada.MarcarContactado(contactado, creadoEn);
        return entrada;
    }

    private ListarListaEsperaHandler Listar() => new(_ctx.DbContext.Object, _ctx.AppSettings.Object);

    [Fact]
    public async Task Listar_OrdersNotContactedFirstThenOldestFirstAndPaginates()
    {
        var now = CatalogoFactory.Now;
        _ctx.SetupListaEspera(
            Entrada("C", now.AddDays(-1)), Entrada("Contactada", now.AddDays(-10), contactado: true),
            Entrada("A", now.AddDays(-5)), Entrada("B", now.AddDays(-3)));

        var page1 = (await Listar().Handle(new ListarListaEsperaQuery(null, null, null, 1, 3), CancellationToken.None)).Value;
        var page2 = (await Listar().Handle(new ListarListaEsperaQuery(null, null, null, 2, 3), CancellationToken.None)).Value;

        page1.Items.Select(i => i.Nombre).Should().Equal("A", "B", "C");
        page2.Items.Select(i => i.Nombre).Should().Equal("Contactada");
        (page1.Total, page1.Page, page1.PageSize).Should().Be((4, 1, 3));
    }

    [Fact]
    public async Task Listar_FiltersByCourseContactedAndAccentInsensitiveSearch()
    {
        var otro = CatalogoFactory.Curso("otro");
        var now = CatalogoFactory.Now;
        _ctx.SetupListaEspera(
            Entrada("José Núñez", now), Entrada("Josefa", now, contactado: true), Entrada("Jose Otro", now, curso: otro),
            Entrada("María", now));

        async Task<IEnumerable<string>> Nombres(ListarListaEsperaQuery q) =>
            (await Listar().Handle(q, CancellationToken.None)).Value.Items.Select(i => i.Nombre);

        (await Nombres(new ListarListaEsperaQuery(_curso.Id, null, "jose"))).Should().BeEquivalentTo("José Núñez", "Josefa");
        (await Nombres(new ListarListaEsperaQuery(_curso.Id, false, "JOSÉ"))).Should().BeEquivalentTo("José Núñez");
        (await Nombres(new ListarListaEsperaQuery(null, null, "nunez"))).Should().BeEquivalentTo("José Núñez");
        (await Nombres(new ListarListaEsperaQuery(_curso.Id, null, "612 34"))).Should().HaveCount(3);
    }

    [Fact]
    public async Task MarcarContactado_SetsTheMarkAndNotesOnlyWhenSent()
    {
        var entrada = Entrada("Lucía", CatalogoFactory.Now.AddDays(-2));
        entrada.CambiarNotas("Nota previa");
        _ctx.SetupListaEspera(entrada);
        var handler = new MarcarContactadoHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object, _ctx.TimeProvider);

        var marcada = (await handler.Handle(
            new MarcarContactadoCommand(entrada.Id, true, Optional<string?>.None()), CancellationToken.None)).Value;
        marcada.Contactado.Should().BeTrue();
        marcada.ContactadoEn.Should().Be(CatalogoFactory.Now);
        marcada.Notas.Should().Be("Nota previa");

        var desmarcada = (await handler.Handle(
            new MarcarContactadoCommand(entrada.Id, false, Optional<string?>.Some("Volver a llamar")), CancellationToken.None)).Value;
        desmarcada.Contactado.Should().BeFalse();
        desmarcada.ContactadoEn.Should().BeNull();
        desmarcada.Notas.Should().Be("Volver a llamar");
    }

    [Fact]
    public async Task MarcarContactado_UnknownEntry_ThrowsNotFound()
    {
        var handler = new MarcarContactadoHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object, _ctx.TimeProvider);

        var act = () => handler.Handle(
            new MarcarContactadoCommand(Guid.NewGuid(), true, Optional<string?>.None()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Exportar_WritesMadridDatesAndNamesTheFileWithSlugAndDate()
    {
        // 23:30 UTC on 3 Nov is 00:30 on 4 Nov in Madrid (CET, UTC+1).
        var creadoEn = new DateTimeOffset(2026, 11, 3, 23, 30, 0, TimeSpan.Zero);
        var edicion = CatalogoFactory.Edicion(_curso, inicio: new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero));
        var entrada = ListaEspera.Create(
            _curso, edicion, "=Sábados", "Lucía; Pérez", "lucia@mail.com", "+34612345678", ConsentimientoListaEspera.V1, creadoEn);
        entrada.CreatedAt = creadoEn;
        _ctx.SetupListaEspera(entrada);
        _ctx.TimeProvider.SetUtcNow(creadoEn);
        var handler = new ExportarListaEsperaHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object, _ctx.TimeProvider);

        var archivo = (await handler.Handle(new ExportarListaEsperaQuery(_curso.Id), CancellationToken.None)).Value;

        archivo.NombreArchivo.Should().Be("lista-espera-acrilico-20261104.csv");
        var lineas = Encoding.UTF8.GetString(archivo.Contenido[3..]).Split("\r\n");
        lineas[0].Should().Be("Curso;Nombre;Teléfono;Email;Fecha preferida;Edición preferida;Contactado;Fecha de alta;Notas;Baja");
        lineas[1].Should().Be("Acrílico;\"Lucía; Pérez\";'+34612345678;lucia@mail.com;'=Sábados;05/12/2026 10:00;No;04/11/2026 00:30;;No");
    }

    [Fact]
    public async Task Exportar_UnknownCourse_ThrowsNotFound()
    {
        var handler = new ExportarListaEsperaHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object, _ctx.TimeProvider);

        var act = () => handler.Handle(new ExportarListaEsperaQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

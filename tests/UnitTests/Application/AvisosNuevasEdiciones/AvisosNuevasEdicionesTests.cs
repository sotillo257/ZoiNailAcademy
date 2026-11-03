using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.AvisosNuevasEdiciones;
using ZoiNailAcademy.Application.AvisosNuevasEdiciones.EnviarAvisos;
using ZoiNailAcademy.Application.AvisosNuevasEdiciones.MarcarCursoPendiente;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.ListasEspera.DarseDeBaja;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.AvisosNuevasEdiciones;

public class AvisoReferenciaTests
{
    [Fact]
    public void Calcular_SameEntryAndEditionsInAnyOrder_GivesTheSameReference()
    {
        var persona = Guid.NewGuid();
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());

        var referencia = AvisoReferencia.Calcular(persona, [a, b]);

        AvisoReferencia.Calcular(persona, [b, a, a]).Should().Be(referencia);
        AvisoReferencia.Calcular(persona, [a]).Should().NotBe(referencia);
        AvisoReferencia.Calcular(Guid.NewGuid(), [a, b]).Should().NotBe(referencia);
    }
}

public class MarcarCursoPendienteHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Edicion _edicion = CatalogoFactory.Edicion();

    public MarcarCursoPendienteHandlerTests() => _ctx.SetupEdiciones(_edicion);

    private MarcarCursoPendienteHandler Handler() =>
        new(_ctx.DbContext.Object, _ctx.TimeProvider, NullLogger<MarcarCursoPendienteHandler>.Instance);

    [Fact]
    public async Task Handle_FirstPublication_MarksTheCourse()
    {
        await Handler().Handle(new EdicionPublicadaEvent(_edicion.Id), CancellationToken.None);

        var pendiente = _ctx.AvisosPendientes.Should().ContainSingle().Subject;
        (pendiente.CursoId, pendiente.PendienteDesde, pendiente.Publicaciones).Should().Be((_edicion.CursoId, CatalogoFactory.Now, 1));
        _ctx.Emails.Should().BeEmpty("nothing is enqueued until the job groups the dates");
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_CourseAlreadyMarked_KeepsTheFirstMomentAndCountsThePublication()
    {
        var existente = AvisoPendiente.Create(_edicion.CursoId, CatalogoFactory.Now.AddMinutes(-5));
        _ctx.SetupAvisosPendientes(existente);

        await Handler().Handle(new EdicionPublicadaEvent(_edicion.Id), CancellationToken.None);

        _ctx.AvisosPendientes.Should().ContainSingle().Which.Should().BeSameAs(existente);
        (existente.PendienteDesde, existente.Publicaciones).Should().Be((CatalogoFactory.Now.AddMinutes(-5), 2));
    }

    [Fact]
    public async Task Handle_UnknownEdition_DoesNothing()
    {
        await Handler().Handle(new EdicionPublicadaEvent(Guid.NewGuid()), CancellationToken.None);

        _ctx.AvisosPendientes.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}

public class EnviarAvisosNuevasEdicionesHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Curso _curso = CatalogoFactory.Curso();

    private EnviarAvisosNuevasEdicionesHandler Handler() => new(
        _ctx.DbContext.Object, _ctx.CreateEdicionesPublicas(), _ctx.CreateBandeja(), _ctx.EmailSettings.Object,
        _ctx.Avisos.Object, _ctx.TimeProvider, NullLogger<EnviarAvisosNuevasEdicionesHandler>.Instance);

    private ListaEspera Persona(string nombre, Curso? curso = null) => ListaEspera.Create(
        curso ?? _curso, null, null, nombre, $"{Guid.NewGuid():N}@mail.test", "+34612345678", "Consentimiento",
        CatalogoFactory.Now.AddDays(-10));

    private Task<int> RunAsync() =>
        Handler().Handle(new EnviarAvisosNuevasEdicionesCommand(), CancellationToken.None).ContinueWith(t => t.Result.Value);

    [Fact]
    public async Task Handle_SendsOneEmailPerSubscribedPersonWithEveryBookableDateAndASummary()
    {
        var primera = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(20));
        var segunda = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(40), plazas: 6);
        var completa = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(30), plazas: 2);
        var oculta = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(25), estado: EstadoEdicion.Oculta);
        var dentroDelCierre = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddHours(12));
        var otroCurso = CatalogoFactory.Curso("gel");
        _ctx.SetupEdiciones(primera, segunda, completa, oculta, dentroDelCierre, CatalogoFactory.Edicion(otroCurso));
        _ctx.Ocupaciones[completa.Id] = new Ocupacion(2, 0);
        _ctx.Ocupaciones[segunda.Id] = new Ocupacion(1, 1);
        _ctx.SetupCursos(_curso, otroCurso);
        var lucia = Persona("Lucía");
        var deBaja = Persona("Baja");
        deBaja.DarDeBaja(CatalogoFactory.Now);
        _ctx.SetupListaEspera(lucia, deBaja, Persona("Otra", otroCurso));
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now.AddMinutes(-3)));

        var avisados = await RunAsync();

        avisados.Should().Be(1);
        var email = _ctx.Emails.Should().ContainSingle(e => e.Tipo == TiposEmail.AvisoNuevasEdiciones).Subject;
        email.Para.Should().Be(lucia.Email);
        email.ReplyTo.Should().Be("zoi@academia.test");
        email.Asunto.Should().Be("Nuevas fechas para Acrílico");
        email.ReferenciaId.Should().Be(AvisoReferencia.Calcular(lucia.Id, [primera.Id, segunda.Id]));
        email.BajaUrl.Should().Be($"https://web.test/api/lista-espera/baja?token={lucia.BajaToken}");
        email.ProximoIntentoEn.Should().Be(CatalogoFactory.Now);
        email.Texto.Should().Contain("Quedan 10 plazas").And.Contain("Quedan 4 plazas").And.NotContain("Quedan 0");
        _ctx.AvisosEnviados.Select(a => (a.ListaEsperaId, a.EdicionId)).Should().BeEquivalentTo(
            [(lucia.Id, primera.Id), (lucia.Id, segunda.Id)]);

        var resumen = _ctx.Emails.Should().ContainSingle(e => e.Tipo == TiposEmail.AvisoNuevasEdicionesClienta).Subject;
        (resumen.Para, resumen.Asunto).Should().Be(("avisos@academia.test", "Aviso enviado a 1 interesado · Acrílico"));
        resumen.BajaUrl.Should().BeNull();
        _ctx.AvisosPendientes.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_DatesAlreadyNotified_AreSkippedAndNobodyWithoutNewDatesIsEmailed()
    {
        var vieja = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(20));
        var nueva = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(40));
        _ctx.SetupEdiciones(vieja, nueva);
        _ctx.SetupCursos(_curso);
        var avisadaDeTodo = Persona("Ya avisada");
        var avisadaDeLaVieja = Persona("Media");
        _ctx.SetupListaEspera(avisadaDeTodo, avisadaDeLaVieja);
        _ctx.SetupAvisosEnviados(
            AvisoEnviado.Create(avisadaDeTodo.Id, vieja.Id, CatalogoFactory.Now.AddDays(-1)),
            AvisoEnviado.Create(avisadaDeTodo.Id, nueva.Id, CatalogoFactory.Now.AddDays(-1)),
            AvisoEnviado.Create(avisadaDeLaVieja.Id, vieja.Id, CatalogoFactory.Now.AddDays(-1)));
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now));

        var avisados = await RunAsync();

        avisados.Should().Be(1);
        var email = _ctx.Emails.Should().ContainSingle(e => e.Tipo == TiposEmail.AvisoNuevasEdiciones).Subject;
        email.Para.Should().Be(avisadaDeLaVieja.Email);
        email.ReferenciaId.Should().Be(AvisoReferencia.Calcular(avisadaDeLaVieja.Id, [nueva.Id]));
    }

    [Fact]
    public async Task Handle_NoBookableDate_SendsNothingAndClearsTheMark()
    {
        var cancelada = CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(20));
        cancelada.Actualizar(cancelada.Inicio, cancelada.Plazas, EstadoEdicion.Cancelada, null, 0, false, CatalogoFactory.Now);
        _ctx.SetupEdiciones(cancelada);
        _ctx.SetupCursos(_curso);
        _ctx.SetupListaEspera(Persona("Lucía"));
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now));

        (await RunAsync()).Should().Be(0);

        _ctx.Emails.Should().BeEmpty("neither the people nor the client get an email");
        _ctx.AvisosEnviados.Should().BeEmpty();
        _ctx.AvisosPendientes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ManyPeople_EnqueuesThemInSpacedBatches()
    {
        _ctx.Avisos.Setup(x => x.LoteTamano).Returns(2);
        _ctx.SetupEdiciones(CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(20)));
        _ctx.SetupCursos(_curso);
        _ctx.SetupListaEspera(Enumerable.Range(1, 5).Select(i => Persona($"Persona {i}")).ToArray());
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now));

        (await RunAsync()).Should().Be(5);

        _ctx.Emails.Where(e => e.Tipo == TiposEmail.AvisoNuevasEdiciones)
            .Select(e => (e.ProximoIntentoEn - CatalogoFactory.Now).TotalSeconds)
            .Should().Equal(0, 0, 2, 2, 4);
        _ctx.Emails.Single(e => e.Tipo == TiposEmail.AvisoNuevasEdicionesClienta).Asunto
            .Should().Be("Aviso enviado a 5 interesados · Acrílico");
    }

    [Fact]
    public async Task Handle_WithoutClientAddress_StillNotifiesTheWaitingList()
    {
        _ctx.EmailSettings.Setup(x => x.AvisosClienta).Returns((string?)null);
        _ctx.SetupEdiciones(CatalogoFactory.Edicion(_curso, CatalogoFactory.Now.AddDays(20)));
        _ctx.SetupCursos(_curso);
        _ctx.SetupListaEspera(Persona("Lucía"));
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now));

        (await RunAsync()).Should().Be(1);

        _ctx.Emails.Should().ContainSingle().Which.Tipo.Should().Be(TiposEmail.AvisoNuevasEdiciones);
    }

    [Fact]
    public async Task Handle_LockHeldByAnotherReplica_DoesNothing()
    {
        _ctx.DbContext.Setup(x => x.TryAdquirirLockAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);
        _ctx.SetupAvisosPendientes(AvisoPendiente.Create(_curso.Id, CatalogoFactory.Now));

        (await RunAsync()).Should().Be(0);

        _ctx.AvisosPendientes.Should().ContainSingle();
    }
}

public class DarseDeBajaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly ListaEspera _entrada = ListaEspera.Create(
        CatalogoFactory.Curso(), null, null, "Lucía", "lucia@mail.test", "+34612345678", "Consentimiento", CatalogoFactory.Now);

    public DarseDeBajaHandlerTests() => _ctx.SetupListaEspera(_entrada);

    private DarseDeBajaHandler Handler() => new(_ctx.DbContext.Object, _ctx.TimeProvider, NullLogger<DarseDeBajaHandler>.Instance);

    [Fact]
    public async Task Handle_ValidToken_UnsubscribesTheEntry()
    {
        var result = await Handler().Handle(new DarseDeBajaCommand($" {_entrada.BajaToken} "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _entrada.BajaEn.Should().Be(CatalogoFactory.Now);
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("token-que-no-existe")]
    public async Task Handle_InvalidToken_SucceedsWithoutChangingAnything(string? token)
    {
        var result = await Handler().Handle(new DarseDeBajaCommand(token), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _entrada.BajaEn.Should().BeNull();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_AlreadyUnsubscribed_KeepsTheFirstMoment()
    {
        _entrada.DarDeBaja(CatalogoFactory.Now.AddDays(-3));

        await Handler().Handle(new DarseDeBajaCommand(_entrada.BajaToken), CancellationToken.None);

        _entrada.BajaEn.Should().Be(CatalogoFactory.Now.AddDays(-3));
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}

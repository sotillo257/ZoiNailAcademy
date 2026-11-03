using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Resumen.ObtenerResumen;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Resumen;

public class ObtenerResumenHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private ObtenerResumenHandler Handler() => new(
        _ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.AppSettings.Object, _ctx.TimeProvider);

    private static Reserva Manual(Edicion edicion, DateTimeOffset expiraEn) =>
        CatalogoFactory.Reserva(edicion, MetodoPago.Manual, expiraEn: expiraEn);

    [Fact]
    public async Task Handle_CountsEachCounterAndExpiringTodayInMadridTime()
    {
        // 23:30 in Madrid (UTC+1 in November).
        var now = new DateTimeOffset(2026, 11, 3, 22, 30, 0, TimeSpan.Zero);
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddDays(30));
        var hoy = Manual(edicion, now.AddMinutes(15));                     // 23:45 Madrid
        var manana = Manual(edicion, now.AddMinutes(45));                  // 00:15 Madrid, tomorrow
        var tarjeta = CatalogoFactory.Reserva(edicion, expiraEn: now.AddMinutes(10));
        var caducada = Manual(edicion, CatalogoFactory.Now.AddHours(1));    // pending but past its expiry
        var revision = CatalogoFactory.Reserva(edicion);
        revision.Expirar(CatalogoFactory.Now);
        revision.RegistrarPagoSinPlaza();
        _ctx.SetupReservas(hoy, manana, tarjeta, caducada, revision);

        var curso = CatalogoFactory.Curso();
        var sinContactar = ListaEspera.Create(curso, null, null, "Ana", "ana@mail.com", "+34612345678", "V1", CatalogoFactory.Now);
        var contactado = ListaEspera.Create(curso, null, null, "Eva", "eva@mail.com", "+34612345679", "V1", CatalogoFactory.Now);
        contactado.MarcarContactado(true, CatalogoFactory.Now);
        _ctx.SetupListaEspera(sinContactar, contactado);
        _ctx.TimeProvider.SetUtcNow(now);

        var resumen = (await Handler().Handle(new ObtenerResumenQuery(), CancellationToken.None)).Value;

        resumen.PendientesDePago.Should().Be(2);
        resumen.PendientesQueCaducanHoy.Should().Be(1);
        resumen.RequierenRevision.Should().Be(1);
        resumen.ListaEsperaSinContactar.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AfterMidnight_CountsTheBookingsThatExpireBeforeTheNextMidnight()
    {
        // 00:30 in Madrid.
        var now = new DateTimeOffset(2026, 11, 3, 23, 30, 0, TimeSpan.Zero);
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddDays(30));
        _ctx.SetupReservas(Manual(edicion, now.AddHours(23).AddMinutes(20)), Manual(edicion, now.AddHours(23).AddMinutes(40)));
        _ctx.TimeProvider.SetUtcNow(now);

        var resumen = (await Handler().Handle(new ObtenerResumenQuery(), CancellationToken.None)).Value;

        resumen.PendientesDePago.Should().Be(2);
        resumen.PendientesQueCaducanHoy.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsTheNextFiveEditionsNotStartedNorCancelledSoonestFirst()
    {
        var curso = CatalogoFactory.Curso();
        var ediciones = Enumerable.Range(1, 7)
            .Select(d => CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddDays(d)))
            .ToList();
        var cancelada = ediciones[0];
        cancelada.Actualizar(cancelada.Inicio, cancelada.Plazas, EstadoEdicion.Cancelada, null, 0, false, CatalogoFactory.Now);
        var oculta = CatalogoFactory.Edicion(curso, CatalogoFactory.Now.AddHours(12), estado: EstadoEdicion.Oculta);
        _ctx.SetupEdiciones([.. ediciones.AsEnumerable().Reverse(), oculta]);
        _ctx.Ocupaciones[ediciones[1].Id] = new Ocupacion(2, 1);

        var resumen = (await Handler().Handle(new ObtenerResumenQuery(), CancellationToken.None)).Value;

        resumen.ProximasEdiciones.Select(e => e.Id).Should()
            .Equal(oculta.Id, ediciones[1].Id, ediciones[2].Id, ediciones[3].Id, ediciones[4].Id);
        resumen.ProximasEdiciones[1].Pagadas.Should().Be(2);
        resumen.ProximasEdiciones[1].Libres.Should().Be(7);
    }
}

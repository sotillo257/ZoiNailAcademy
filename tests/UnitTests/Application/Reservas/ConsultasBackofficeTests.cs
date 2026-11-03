using System.Text;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Reservas.ExportarAsistentes;
using ZoiNailAcademy.Application.Reservas.ListarPendientes;
using ZoiNailAcademy.Application.Reservas.ListarReservasEdicion;
using ZoiNailAcademy.Application.Reservas.MarcarRevisada;
using ZoiNailAcademy.Application.Reservas.ObtenerReservaAdmin;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Reservas;

public class ListarReservasEdicionTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Edicion _edicion = CatalogoFactory.Edicion();

    public ListarReservasEdicionTests() => _ctx.SetupEdiciones(_edicion);

    private ListarReservasEdicionHandler Handler() =>
        new(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider);

    private Reserva Pagada(int minutos)
    {
        var r = CatalogoFactory.Reserva(_edicion);
        r.Pagar(CatalogoFactory.Now.AddMinutes(minutos));
        return r;
    }

    [Fact]
    public async Task Handle_Default_ReturnsPaidByPaymentDateThenPendingByExpiry()
    {
        var pagadaTarde = Pagada(20);
        var pagadaPronto = Pagada(5);
        var pendienteTarde = CatalogoFactory.Reserva(_edicion, MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(48));
        var pendientePronto = CatalogoFactory.Reserva(_edicion);
        var expirada = CatalogoFactory.Reserva(_edicion);
        expirada.Expirar(CatalogoFactory.Now);
        var otraEdicion = CatalogoFactory.Reserva();
        _ctx.SetupReservas(pendienteTarde, expirada, pagadaTarde, otraEdicion, pendientePronto, pagadaPronto);
        _ctx.Ocupaciones[_edicion.Id] = new Ocupacion(2, 2);

        var result = (await Handler().Handle(new ListarReservasEdicionQuery(_edicion.Id, null), CancellationToken.None)).Value;

        result.Edicion.Id.Should().Be(_edicion.Id);
        result.Edicion.Libres.Should().Be(6);
        result.Reservas.Select(r => r.Id).Should()
            .Equal(pagadaPronto.Id, pagadaTarde.Id, pendientePronto.Id, pendienteTarde.Id);
        result.Reservas.Should().OnlyContain(r => r.PlazaDisponible == null && r.Curso == "Acrílico");
    }

    [Fact]
    public async Task Handle_IncluirExpiredAndCancelled_ReturnsOnlyThose()
    {
        var pagada = Pagada(5);
        var expirada = CatalogoFactory.Reserva(_edicion);
        expirada.Expirar(CatalogoFactory.Now);
        var cancelada = CatalogoFactory.Reserva(_edicion);
        cancelada.Cancelar(CatalogoFactory.Now);
        _ctx.SetupReservas(cancelada, pagada, expirada);

        var result = (await Handler().Handle(
            new ListarReservasEdicionQuery(_edicion.Id, " Expiradas,canceladas "), CancellationToken.None)).Value;

        result.Reservas.Select(r => r.Estado).Should().Equal(EstadoReserva.Expirada, EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Handle_UnknownEdition_ThrowsNotFound()
    {
        var act = () => Handler().Handle(new ListarReservasEdicionQuery(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("pagadas,pendientes,expiradas,canceladas", true)]
    [InlineData("PAGADAS", true)]
    [InlineData("pagadas,todas", false)]
    [InlineData("Pagada", false)]
    public void Validator_Incluir_AcceptsOnlyTheKnownValues(string? incluir, bool valido)
    {
        var result = new ListarReservasEdicionValidator().TestValidate(new ListarReservasEdicionQuery(Guid.NewGuid(), incluir));

        result.IsValid.Should().Be(valido);
    }
}

public class ListarPendientesTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Edicion _edicion = CatalogoFactory.Edicion(plazas: 3);

    private ListarPendientesHandler Handler() => new(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.TimeProvider);

    private Task<IReadOnlyList<ZoiNailAcademy.Application.Reservas.ReservaAdminDto>> ListarAsync(
        string? q = null, MetodoPago? metodo = null) =>
        Handler().Handle(new ListarPendientesQuery(q, metodo), CancellationToken.None).ContinueWith(t => t.Result.Value);

    private Reserva Manual(string? telefono = null, DateTimeOffset? expiraEn = null, Edicion? edicion = null) =>
        CatalogoFactory.Reserva(edicion ?? _edicion, MetodoPago.Manual, telefono: telefono,
            expiraEn: expiraEn ?? CatalogoFactory.Now.AddHours(48));

    private Reserva ExpiradaHace(TimeSpan hace, MetodoPago metodo = MetodoPago.Manual, Edicion? edicion = null)
    {
        var r = CatalogoFactory.Reserva(edicion ?? _edicion, metodo);
        r.Expirar(CatalogoFactory.Now - hace);
        return r;
    }

    [Fact]
    public async Task Handle_ListsReviewFirstThenPendingByExpiryThenRecentExpiredManualOnes()
    {
        var pendienteTarde = Manual(expiraEn: CatalogoFactory.Now.AddHours(40));
        var pendientePronto = CatalogoFactory.Reserva(_edicion);
        var caducadaSinJob = Manual(expiraEn: CatalogoFactory.Now.AddMinutes(1));
        var hace1Dia = ExpiradaHace(TimeSpan.FromDays(1));
        var hace3Dias = ExpiradaHace(TimeSpan.FromDays(3));
        var hace10Dias = ExpiradaHace(TimeSpan.FromDays(10));
        var tarjetaExpirada = ExpiradaHace(TimeSpan.FromDays(1), MetodoPago.Stripe);
        var revision = ExpiradaHace(TimeSpan.FromDays(20), MetodoPago.Stripe);
        revision.RegistrarPagoSinPlaza();
        var pagada = Manual();
        pagada.Pagar(CatalogoFactory.Now);
        _ctx.SetupReservas(hace3Dias, pendienteTarde, hace10Dias, revision, tarjetaExpirada, pagada, caducadaSinJob, hace1Dia, pendientePronto);
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(2));

        var result = await ListarAsync();

        result.Select(r => r.Id).Should().Equal(revision.Id, pendientePronto.Id, pendienteTarde.Id, hace1Dia.Id, hace3Dias.Id);
    }

    [Fact]
    public async Task Handle_ExpiredBookings_SayWhetherTheirEditionHasAFreeSeat()
    {
        var llena = CatalogoFactory.Edicion(plazas: 1);
        var conPlaza = ExpiradaHace(TimeSpan.FromDays(1));
        var sinPlaza = ExpiradaHace(TimeSpan.FromDays(1), edicion: llena);
        var pendiente = Manual();
        _ctx.SetupReservas(conPlaza, sinPlaza, pendiente);
        _ctx.Ocupaciones[_edicion.Id] = new Ocupacion(1, 1);
        _ctx.Ocupaciones[llena.Id] = new Ocupacion(1, 0);

        var result = await ListarAsync();

        result.Single(r => r.Id == conPlaza.Id).PlazaDisponible.Should().BeTrue();
        result.Single(r => r.Id == sinPlaza.Id).PlazaDisponible.Should().BeFalse();
        result.Single(r => r.Id == pendiente.Id).PlazaDisponible.Should().BeNull();
        _ctx.OcupacionReader.Verify(x => x.LeerAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(_edicion.Id) && ids.Contains(llena.Id)),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData("{0}")]
    [InlineData("{1}")]
    [InlineData("cur {2}")]
    [InlineData("{3}")]
    public async Task Handle_CodeTypedInDifferentWays_FindsTheBooking(string formato)
    {
        var buscada = Manual();
        _ctx.SetupReservas(Manual(), buscada, Manual());
        var aleatorio = buscada.Codigo[4..];
        var q = string.Format(formato, buscada.Codigo, buscada.Codigo.ToLowerInvariant(), aleatorio.ToLowerInvariant(), aleatorio);

        var result = await ListarAsync(q);

        result.Should().ContainSingle().Which.Id.Should().Be(buscada.Id);
    }

    [Theory]
    [InlineData("612345678")]
    [InlineData("612 34 56 78")]
    [InlineData("+34 612 345 678")]
    public async Task Handle_PhoneWithoutPrefixOrWithSpaces_FindsTheBooking(string q)
    {
        var buscada = Manual(telefono: "+34612345678");
        _ctx.SetupReservas(buscada, Manual(telefono: "+34698765432"));

        (await ListarAsync(q)).Should().ContainSingle().Which.Id.Should().Be(buscada.Id);
    }

    [Fact]
    public async Task Handle_NameWithoutAccentsOrEmailInCapitals_FindsTheBooking()
    {
        var buscada = Manual();
        _ctx.SetupReservas(buscada);

        (await ListarAsync("lucia")).Should().ContainSingle();
        (await ListarAsync("LUCIA@MAIL")).Should().ContainSingle();
        (await ListarAsync("maría")).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MethodFilter_KeepsOnlyThatMethod()
    {
        var manual = Manual();
        _ctx.SetupReservas(manual, CatalogoFactory.Reserva(_edicion));

        (await ListarAsync(metodo: MetodoPago.Manual)).Should().ContainSingle().Which.Id.Should().Be(manual.Id);
    }
}

public class DetalleYRevisionTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task ObtenerReservaAdmin_AnyState_ReturnsTheBookingOrNotFound()
    {
        var cancelada = CatalogoFactory.Reserva();
        cancelada.Cancelar(CatalogoFactory.Now);
        _ctx.SetupReservas(cancelada);
        var handler = new ObtenerReservaAdminHandler(_ctx.DbContext.Object);

        var dto = (await handler.Handle(new ObtenerReservaAdminQuery(cancelada.Id), CancellationToken.None)).Value;
        var act = () => handler.Handle(new ObtenerReservaAdminQuery(Guid.NewGuid()), CancellationToken.None);

        dto.Codigo.Should().Be(cancelada.Codigo);
        dto.Estado.Should().Be(EstadoReserva.Cancelada);
        dto.Version.Should().Be("0");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarcarRevisada_ClearsTheMarkSavesAndReturnsTheBooking()
    {
        var reserva = CatalogoFactory.Reserva();
        reserva.Expirar(CatalogoFactory.Now);
        reserva.RegistrarPagoSinPlaza();
        _ctx.SetupReservas(reserva);

        var result = await new MarcarRevisadaHandler(_ctx.DbContext.Object)
            .Handle(new MarcarRevisadaCommand(reserva.Id, "0", "Reembolsado"), CancellationToken.None);

        result.Value.Reserva.RequiereRevision.Should().BeFalse();
        result.Value.Reserva.NotaInterna.Should().Be("Reembolsado");
        result.Value.Avisos.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task MarcarRevisada_StaleVersion_ThrowsConflictAndDoesNotSave()
    {
        var reserva = CatalogoFactory.Reserva();
        _ctx.SetupReservas(reserva);

        var act = () => new MarcarRevisadaHandler(_ctx.DbContext.Object)
            .Handle(new MarcarRevisadaCommand(reserva.Id, "7", null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData("", null, false)]
    [InlineData("12", null, true)]
    [InlineData("12", "ok", true)]
    public void MarcarRevisadaValidator_RequiresANumericVersion(string version, string? nota, bool valido)
    {
        new MarcarRevisadaValidator().TestValidate(new MarcarRevisadaCommand(Guid.NewGuid(), version, nota))
            .IsValid.Should().Be(valido);
    }

    [Fact]
    public void MarcarRevisadaValidator_NoteTooLong_Fails()
    {
        new MarcarRevisadaValidator().TestValidate(new MarcarRevisadaCommand(Guid.NewGuid(), "1", new string('a', 501)))
            .ShouldHaveValidationErrorFor(x => x.Nota);
    }
}

public class ExportarAsistentesTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task Handle_ExportsOnlyPaidBookingsInMadridTimeWithDecimalComma()
    {
        var curso = CatalogoFactory.Curso("acrilico", precio: 149.5m);
        var edicion = CatalogoFactory.Edicion(curso, new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero));
        var tarjeta = CatalogoFactory.Reserva(edicion);
        tarjeta.Pagar(new DateTimeOffset(2026, 11, 3, 23, 30, 0, TimeSpan.Zero));
        var manual = CatalogoFactory.Reserva(edicion, MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(48));
        manual.ConfirmarPagoManual(CatalogoFactory.Now.AddMinutes(5), true, null);
        var pendiente = CatalogoFactory.Reserva(edicion);
        _ctx.SetupEdiciones(edicion);
        _ctx.SetupReservas(tarjeta, pendiente, manual);

        var archivo = (await new ExportarAsistentesHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object)
            .Handle(new ExportarAsistentesQuery(edicion.Id), CancellationToken.None)).Value;

        archivo.NombreArchivo.Should().Be("asistentes-acrilico-20261205-1000.csv");
        archivo.Contenido.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var lineas = Encoding.UTF8.GetString(archivo.Contenido[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lineas.Should().Equal(
            "Código;Nombre;Teléfono;Email;Método;Importe;Fecha de pago",
            $"{manual.Codigo};Lucía Pérez;'+34612345678;lucia@mail.com;Transferencia o Bizum;149,50;03/11/2026 11:05",
            $"{tarjeta.Codigo};Lucía Pérez;'+34612345678;lucia@mail.com;Tarjeta;149,50;04/11/2026 00:30");
    }

    [Fact]
    public async Task Handle_UnknownEdition_ThrowsNotFound()
    {
        var act = () => new ExportarAsistentesHandler(_ctx.DbContext.Object, _ctx.AppSettings.Object)
            .Handle(new ExportarAsistentesQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

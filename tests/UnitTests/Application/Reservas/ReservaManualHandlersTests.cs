using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.AmpliarReserva;
using ZoiNailAcademy.Application.Reservas.CancelarReserva;
using ZoiNailAcademy.Application.Reservas.ConfirmarReserva;
using ZoiNailAcademy.Application.Reservas.ExpirarReservasCaducadas;
using ZoiNailAcademy.Application.Reservas.ReservarManual;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Reservas;

public class ReservarManualHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private ReservarManualHandler Handler() => new(
        _ctx.DbContext.Object,
        new CrearReservaService(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider),
        _ctx.CreateDtoFactory(), _ctx.Publisher.Object, _ctx.Settings.Object, _ctx.TimeProvider,
        NullLogger<ReservarManualHandler>.Instance);

    private static ReservarManualCommand Command(Guid edicionId, string? website = null) =>
        new(edicionId, "Lucía", "lucia@mail.com", "612345678", true, website);

    [Fact]
    public async Task Handle_FarEdition_ExpiresAfter48HoursAndReturnsPaymentDetails()
    {
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddDays(30));
        _ctx.SetupEdiciones(edicion);

        var result = await Handler().Handle(Command(edicion.Id), CancellationToken.None);

        var reserva = _ctx.Reservas.Should().ContainSingle().Subject;
        reserva.Metodo.Should().Be(MetodoPago.Manual);
        reserva.ExpiraEn.Should().Be(CatalogoFactory.Now.AddHours(48));
        result.Value.DatosPago.Should().Be(new DatosPagoDto("Zoi Nail Academy", "ES12 3456", "600111222", reserva.Codigo));
        result.Value.WhatsappUrl.Should().StartWith("https://wa.me/34600111222?text=");
        result.Value.ExpiraEn.Should().Be(reserva.ExpiraEn);
        _ctx.VerifyPublished<ReservaManualCreadaEvent>(e => e.ReservaId == reserva.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_EditionClosingSoon_ClipsExpiryToTheClosingTime()
    {
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddHours(34)); // closes in 10 h
        _ctx.SetupEdiciones(edicion);

        await Handler().Handle(Command(edicion.Id), CancellationToken.None);

        _ctx.Reservas.Should().ContainSingle().Which.ExpiraEn.Should().Be(CatalogoFactory.Now.AddHours(10));
    }

    [Fact]
    public async Task Handle_LessThanTwoHoursToClosing_ThrowsAndDoesNotCommit()
    {
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddHours(25)); // closes in 1 h
        _ctx.SetupEdiciones(edicion);

        var act = () => Handler().Handle(Command(edicion.Id), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<BusinessRuleException>()).Which;
        ex.Code.Should().Be(ErrorCodes.EdicionNoReservable);
        ex.Message.Should().Be("Para esta fecha solo se puede pagar con tarjeta");
        _ctx.VerifyCommitted(Times.Never());
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_Honeypot_ReturnsPlausibleAnswerAndStoresNothing()
    {
        var edicion = CatalogoFactory.Edicion();
        _ctx.SetupEdiciones(edicion);

        var result = await Handler().Handle(Command(edicion.Id, website: "spam"), CancellationToken.None);

        result.Value.Curso.Should().Be(edicion.Curso.Nombre);
        result.Value.DatosPago.Should().NotBeNull();
        _ctx.Reservas.Should().BeEmpty();
        _ctx.VerifyNothingPublished();
    }
}

public class ConfirmarReservaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Edicion _edicion = CatalogoFactory.Edicion(plazas: 1);

    private ConfirmarReservaHandler Handler() => new(
        _ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Publisher.Object, _ctx.TimeProvider,
        NullLogger<ConfirmarReservaHandler>.Instance);

    private Reserva Manual()
    {
        var reserva = CatalogoFactory.Reserva(_edicion, MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(48));
        _ctx.SetupReservas(reserva);
        return reserva;
    }

    [Fact]
    public async Task Handle_PendingManual_PaysCommitsAndPublishes()
    {
        var reserva = Manual();

        var result = await Handler().Handle(new ConfirmarReservaCommand(reserva.Id, "0", "Bizum"), CancellationToken.None);

        result.Value.Reserva.Estado.Should().Be(EstadoReserva.Pagada);
        result.Value.Reserva.NotaInterna.Should().Be("Bizum");
        _ctx.VerifyCommitted(Times.Once());
        _ctx.VerifyPublished<ReservaPagadaEvent>(e => e.ReservaId == reserva.Id, Times.Once());
        _ctx.DbContext.Verify(x => x.BloquearEdicionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never,
            "a booking that still holds its seat needs no seat check");
    }

    [Fact]
    public async Task Handle_StaleVersion_ThrowsConcurrencia()
    {
        var reserva = Manual();

        var act = () => Handler().Handle(new ConfirmarReservaCommand(reserva.Id, "9", null), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be(ErrorCodes.Concurrencia);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task Handle_ExpiredWithoutSeat_LocksEditionAndThrowsSinPlazas()
    {
        var reserva = Manual();
        reserva.Expirar(CatalogoFactory.Now);
        _ctx.Ocupaciones[_edicion.Id] = new Ocupacion(1, 0);

        var act = () => Handler().Handle(new ConfirmarReservaCommand(reserva.Id, "0", null), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.SinPlazas);
        _ctx.DbContext.Verify(x => x.BloquearEdicionAsync(_edicion.Id, It.IsAny<CancellationToken>()), Times.Once);
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_AlreadyPaid_ReturnsWithoutNewEvent()
    {
        var reserva = Manual();
        reserva.Pagar(CatalogoFactory.Now);

        await Handler().Handle(new ConfirmarReservaCommand(reserva.Id, "0", null), CancellationToken.None);

        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_SaveConflict_ThrowsConcurrencia()
    {
        var reserva = Manual();
        _ctx.DbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("xmin"));

        var act = () => Handler().Handle(new ConfirmarReservaCommand(reserva.Id, "0", null), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be(ErrorCodes.Concurrencia);
        _ctx.VerifyNothingPublished();
    }
}

public class CancelarReservaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private CancelarReservaHandler Handler() => new(
        _ctx.DbContext.Object, _ctx.Stripe.Object, _ctx.Publisher.Object, _ctx.TimeProvider,
        NullLogger<CancelarReservaHandler>.Instance);

    [Fact]
    public async Task Handle_PendingCard_ExpiresSessionCancelsAndPublishes()
    {
        var reserva = CatalogoFactory.Reserva();
        reserva.AsignarSesionStripe("cs_1");
        _ctx.SetupReservas(reserva);
        _ctx.Stripe.Setup(x => x.ExpirarSesionAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(ResultadoExpiracionSesion.Expirada);

        var result = await Handler().Handle(new CancelarReservaCommand(reserva.Id, "0", "Duplicada", false), CancellationToken.None);

        result.Value.Reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        result.Value.Avisos.Should().BeEmpty();
        _ctx.VerifyPublished<ReservaCanceladaEvent>(e => e.ReservaId == reserva.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_CardSessionJustCompleted_ThrowsEstadoInvalidoWithoutCancelling()
    {
        var reserva = CatalogoFactory.Reserva();
        reserva.AsignarSesionStripe("cs_1");
        _ctx.SetupReservas(reserva);
        _ctx.Stripe.Setup(x => x.ExpirarSesionAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(ResultadoExpiracionSesion.YaCompletada);

        var act = () => Handler().Handle(new CancelarReservaCommand(reserva.Id, "0", null, false), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task Handle_PaidWithConfirmation_WarnsToRefund()
    {
        var reserva = CatalogoFactory.Reserva(metodo: MetodoPago.Manual);
        reserva.Pagar(CatalogoFactory.Now);
        _ctx.SetupReservas(reserva);

        var result = await Handler().Handle(new CancelarReservaCommand(reserva.Id, "0", null, true), CancellationToken.None);

        result.Value.Avisos.Should().Equal(CancelarReservaHandler.AvisoReembolso);
        _ctx.Stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_PublishesNothing()
    {
        var reserva = CatalogoFactory.Reserva(metodo: MetodoPago.Manual);
        reserva.Cancelar(CatalogoFactory.Now);
        _ctx.SetupReservas(reserva);

        await Handler().Handle(new CancelarReservaCommand(reserva.Id, "0", null, false), CancellationToken.None);

        _ctx.VerifyNothingPublished();
    }
}

public class AmpliarReservaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private AmpliarReservaHandler Handler() =>
        new(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider);

    [Fact]
    public async Task Handle_ClipsToTheEditionClosingAndWarns()
    {
        var edicion = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddHours(124)); // closes in 100 h
        var reserva = CatalogoFactory.Reserva(edicion, MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(48));
        _ctx.SetupReservas(reserva);

        var result = await Handler().Handle(new AmpliarReservaCommand(reserva.Id, "0", 168), CancellationToken.None);

        result.Value.Reserva.ExpiraEn.Should().Be(CatalogoFactory.Now.AddHours(100));
        result.Value.Avisos.Should().Equal(AmpliarReservaHandler.AvisoRecortada);
        _ctx.VerifyCommitted(Times.Once());
    }

    [Fact]
    public async Task Handle_ExpiredWithSeat_LocksEditionAndReactivates()
    {
        var edicion = CatalogoFactory.Edicion();
        var reserva = CatalogoFactory.Reserva(edicion, MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(1));
        reserva.Expirar(CatalogoFactory.Now);
        _ctx.SetupReservas(reserva);

        var result = await Handler().Handle(new AmpliarReservaCommand(reserva.Id, "0", 24), CancellationToken.None);

        result.Value.Reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        result.Value.Reserva.ExpiraEn.Should().Be(CatalogoFactory.Now.AddHours(24));
        _ctx.DbContext.Verify(x => x.BloquearEdicionAsync(edicion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ExpirarReservasCaducadasHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    private ExpirarReservasCaducadasHandler Handler() => new(
        _ctx.DbContext.Object, _ctx.Stripe.Object, _ctx.Publisher.Object, _ctx.TimeProvider,
        NullLogger<ExpirarReservasCaducadasHandler>.Instance);

    [Fact]
    public async Task Handle_ExpiresDueBookingsOnlyAndPublishesOneEventEach()
    {
        var vencida = CatalogoFactory.Reserva(metodo: MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddMinutes(10));
        var vigente = CatalogoFactory.Reserva(metodo: MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddHours(10));
        _ctx.SetupReservas(vencida, vigente);
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(11));

        var result = await Handler().Handle(new ExpirarReservasCaducadasCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        vencida.Estado.Should().Be(EstadoReserva.Expirada);
        vigente.Estado.Should().Be(EstadoReserva.Pendiente);
        _ctx.VerifyPublished<ReservaExpiradaEvent>(e => e.ReservaId == vencida.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_StripeSessionAlreadyCompleted_LeavesItPending()
    {
        var tarjeta = CatalogoFactory.Reserva(expiraEn: CatalogoFactory.Now.AddMinutes(10));
        tarjeta.AsignarSesionStripe("cs_1");
        _ctx.SetupReservas(tarjeta);
        _ctx.Stripe.Setup(x => x.ExpirarSesionAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(ResultadoExpiracionSesion.YaCompletada);
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(11));

        var result = await Handler().Handle(new ExpirarReservasCaducadasCommand(), CancellationToken.None);

        result.Value.Should().Be(0);
        tarjeta.Estado.Should().Be(EstadoReserva.Pendiente);
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_OneFailingSave_DoesNotStopTheBatch()
    {
        var primera = CatalogoFactory.Reserva(metodo: MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddMinutes(5));
        var segunda = CatalogoFactory.Reserva(metodo: MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddMinutes(10));
        _ctx.SetupReservas(primera, segunda);
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(11));
        _ctx.DbContext.SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("xmin"))
            .ReturnsAsync(1);

        var result = await Handler().Handle(new ExpirarReservasCaducadasCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        _ctx.DbContext.Verify(x => x.DescartarCambios(), Times.Once);
        _ctx.VerifyPublished<ReservaExpiradaEvent>(e => e.ReservaId == segunda.Id, Times.Once());
        _ctx.VerifyPublished<ReservaExpiradaEvent>(e => e.ReservaId == primera.Id, Times.Never());
    }

    [Fact]
    public async Task Handle_LockHeldByAnotherReplica_DoesNothing()
    {
        var vencida = CatalogoFactory.Reserva(metodo: MetodoPago.Manual, expiraEn: CatalogoFactory.Now.AddMinutes(5));
        _ctx.SetupReservas(vencida);
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(11));
        _ctx.DbContext.Setup(x => x.TryAdquirirLockAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var result = await Handler().Handle(new ExpirarReservasCaducadasCommand(), CancellationToken.None);

        result.Value.Should().Be(0);
        vencida.Estado.Should().Be(EstadoReserva.Pendiente);
    }
}

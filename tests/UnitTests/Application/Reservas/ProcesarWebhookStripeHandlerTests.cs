using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.ProcesarWebhookStripe;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Reservas;

public class ProcesarWebhookStripeHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Mock<IStripeWebhookParser> _parser = new();
    private readonly ProcesarWebhookStripeHandler _handler;
    private readonly Edicion _edicion = CatalogoFactory.Edicion(plazas: 1);
    private readonly Reserva _reserva;

    public ProcesarWebhookStripeHandlerTests()
    {
        _reserva = CatalogoFactory.Reserva(_edicion);
        _reserva.AsignarSesionStripe("cs_1");
        _ctx.SetupEdiciones(_edicion);
        _ctx.SetupReservas(_reserva);
        _handler = new ProcesarWebhookStripeHandler(
            _ctx.DbContext.Object, _parser.Object, _ctx.OcupacionReader.Object, _ctx.Publisher.Object, _ctx.TimeProvider,
            NullLogger<ProcesarWebhookStripeHandler>.Instance);
    }

    private Task Recibir(string tipo, string? sessionId = "cs_1", string? paymentStatus = "paid", string id = "evt_1")
    {
        _parser.Setup(x => x.Parse("{}", "firma"))
            .Returns(new StripeWebhookEvento(id, tipo, sessionId, _reserva.Id.ToString(), paymentStatus, "pi_1"));
        return _handler.Handle(new ProcesarWebhookStripeCommand("{}", "firma"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_CompletedForPending_PaysRecordsEventAndPublishes()
    {
        await Recibir(StripeWebhookEvento.CheckoutCompletado);

        _reserva.Estado.Should().Be(EstadoReserva.Pagada);
        _reserva.StripePaymentIntentId.Should().Be("pi_1");
        _ctx.StripeEventos.Should().ContainSingle().Which.EventId.Should().Be("evt_1");
        _ctx.VerifyCommitted(Times.Once());
        _ctx.VerifyPublished<ReservaPagadaEvent>(e => e.ReservaId == _reserva.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_DuplicateEvent_ChangesNothing()
    {
        _ctx.SetupStripeEventos(StripeEvento.Create("evt_1", StripeWebhookEvento.CheckoutCompletado, CatalogoFactory.Now));

        await Recibir(StripeWebhookEvento.CheckoutCompletado);

        _reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        _ctx.VerifyCommitted(Times.Never());
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_UnknownSessionAndReference_RecordsEventAndPublishesNothing()
    {
        _ctx.SetupReservas();

        await Recibir(StripeWebhookEvento.CheckoutCompletado, sessionId: "cs_otro");

        _ctx.StripeEventos.Should().ContainSingle();
        _ctx.VerifyCommitted(Times.Once());
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_CompletedButUnpaid_DoesNotPay()
    {
        await Recibir(StripeWebhookEvento.CheckoutCompletado, paymentStatus: "unpaid");

        _reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_OtherEventType_IsIgnoredWithoutTransaction()
    {
        await Recibir("payment_intent.created");

        _ctx.DbContext.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _ctx.StripeEventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CompletedForPaid_DoesNotPublishAgain()
    {
        _reserva.Pagar(CatalogoFactory.Now);

        await Recibir(StripeWebhookEvento.CheckoutCompletado);

        _ctx.VerifyNothingPublished();
    }

    [Fact]
    public async Task Handle_FindsBookingByClientReferenceWhenSessionIsUnknown()
    {
        await Recibir(StripeWebhookEvento.CheckoutCompletado, sessionId: null);

        _reserva.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public async Task Handle_Expired_ExpiresPendingAndPublishes()
    {
        await Recibir(StripeWebhookEvento.CheckoutExpirado, paymentStatus: "unpaid");

        _reserva.Estado.Should().Be(EstadoReserva.Expirada);
        _ctx.VerifyPublished<ReservaExpiradaEvent>(e => e.ReservaId == _reserva.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_LatePaymentWithFreeSeat_LocksEditionAndPays()
    {
        _reserva.Expirar(CatalogoFactory.Now);

        await Recibir(StripeWebhookEvento.CheckoutCompletado);

        _reserva.Estado.Should().Be(EstadoReserva.Pagada);
        _ctx.DbContext.Verify(x => x.BloquearEdicionAsync(_edicion.Id, It.IsAny<CancellationToken>()), Times.Once);
        _ctx.VerifyPublished<ReservaPagadaEvent>(e => e.ReservaId == _reserva.Id, Times.Once());
    }

    [Fact]
    public async Task Handle_LatePaymentWithoutSeat_FlagsReviewAndPublishesRefundEvent()
    {
        _reserva.Cancelar(CatalogoFactory.Now);
        _ctx.Ocupaciones[_edicion.Id] = new Ocupacion(1, 0);

        await Recibir(StripeWebhookEvento.CheckoutCompletado);

        _reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        _reserva.RequiereRevision.Should().BeTrue();
        _ctx.VerifyPublished<PagoTrasCaducidadSinPlazaEvent>(e => e.ReservaId == _reserva.Id, Times.Once());
        _ctx.VerifyPublished<ReservaPagadaEvent>(_ => true, Times.Never());
    }

    [Fact]
    public async Task Handle_FailingEventHandler_DoesNotFailTheWebhook()
    {
        _ctx.Publisher.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp caído"));

        var act = () => Recibir(StripeWebhookEvento.CheckoutCompletado);

        await act.Should().NotThrowAsync();
        _reserva.Estado.Should().Be(EstadoReserva.Pagada);
    }
}

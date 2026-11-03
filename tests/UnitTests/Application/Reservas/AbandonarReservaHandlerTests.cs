using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Reservas.AbandonarReserva;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Reservas;

public class AbandonarReservaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Mock<IStripeGateway> _stripe = new();
    private readonly AbandonarReservaHandler _handler;
    private readonly Reserva _reserva = CatalogoFactory.Reserva();

    public AbandonarReservaHandlerTests()
    {
        _reserva.AsignarSesionStripe("cs_1");
        _ctx.SetupReservas(_reserva);
        _handler = new AbandonarReservaHandler(_ctx.DbContext.Object, _stripe.Object, _ctx.TimeProvider);
    }

    [Fact]
    public async Task Handle_Pending_ExpiresSessionAndCancels()
    {
        _stripe.Setup(x => x.ExpirarSesionAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(ResultadoExpiracionSesion.Expirada);

        await _handler.Handle(new AbandonarReservaCommand(_reserva.Codigo), CancellationToken.None);

        _reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SessionAlreadyCompleted_LeavesBookingPending()
    {
        _stripe.Setup(x => x.ExpirarSesionAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(ResultadoExpiracionSesion.YaCompletada);

        await _handler.Handle(new AbandonarReservaCommand(_reserva.Codigo), CancellationToken.None);

        _reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task Handle_Paid_DoesNothing()
    {
        _reserva.Pagar(CatalogoFactory.Now);

        await _handler.Handle(new AbandonarReservaCommand(_reserva.Codigo), CancellationToken.None);

        _reserva.Estado.Should().Be(EstadoReserva.Pagada);
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnknownCode_ThrowsNotFound()
    {
        var act = () => _handler.Handle(new AbandonarReservaCommand("CUR-ZZZZZZ"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

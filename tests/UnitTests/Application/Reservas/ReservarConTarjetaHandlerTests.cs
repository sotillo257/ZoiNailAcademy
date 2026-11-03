using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Application.Reservas.ReservarConTarjeta;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace UnitTests.Application.Reservas;

public class ReservarConTarjetaHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Mock<IStripeGateway> _stripe = new();
    private readonly ReservarConTarjetaHandler _handler;
    private readonly Edicion _edicion = CatalogoFactory.Edicion(
        CatalogoFactory.Curso(precio: 149.90m), new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero));

    public ReservarConTarjetaHandlerTests()
    {
        _ctx.SetupEdiciones(_edicion);
        _handler = new ReservarConTarjetaHandler(
            _ctx.DbContext.Object,
            new CrearReservaService(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider),
            _stripe.Object, _ctx.Settings.Object, _ctx.AppSettings.Object, _ctx.TimeProvider,
            NullLogger<ReservarConTarjetaHandler>.Instance);
    }

    private ReservarConTarjetaCommand Command(string? website = null) =>
        new(_edicion.Id, "Lucía", "lucia@mail.com", "612345678", true, website);

    [Fact]
    public async Task Handle_Success_CreatesSessionWithPriceInCentsAndStoresIt()
    {
        CheckoutSesionRequest? enviada = null;
        _stripe.Setup(x => x.CrearSesionCheckoutAsync(It.IsAny<CheckoutSesionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CheckoutSesionRequest, CancellationToken>((r, _) => enviada = r)
            .ReturnsAsync(new CheckoutSesion("cs_1", "https://checkout.stripe.test/cs_1"));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        var reserva = _ctx.Reservas.Should().ContainSingle().Subject;
        result.Value.Should().Be(new ReservaTarjetaCreadaDto(reserva.Codigo, "https://checkout.stripe.test/cs_1"));
        reserva.StripeSessionId.Should().Be("cs_1");
        reserva.ExpiraEn.Should().Be(CatalogoFactory.Now.AddMinutes(35));
        enviada!.ImporteCentimos.Should().Be(14990);
        enviada.ExpiraEn.Should().Be(CatalogoFactory.Now.AddMinutes(30));
        enviada.Concepto.Should().Be("Acrílico — 05/12/2026 10:00");
        enviada.IdempotencyKey.Should().Be($"checkout-{reserva.Id}");
        enviada.SuccessUrl.Should().Be($"https://web.test/reserva/{reserva.Codigo}?origen=stripe");
        enviada.CancelUrl.Should().Be($"https://web.test/reservar/acrilico?abandonada={reserva.Codigo}");
    }

    [Fact]
    public async Task Handle_StripeFails_CancelsBookingAndRethrows()
    {
        _stripe.Setup(x => x.CrearSesionCheckoutAsync(It.IsAny<CheckoutSesionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentProviderException("caída"));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentProviderException>();
        _ctx.Reservas.Should().ContainSingle().Which.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Handle_Honeypot_ReturnsFakeCodeAndCreatesNothing()
    {
        var result = await _handler.Handle(Command(website: "http://spam"), CancellationToken.None);

        CodigoReserva.EsValido(result.Value.Codigo).Should().BeTrue();
        _ctx.Reservas.Should().BeEmpty();
        _ctx.DbContext.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _stripe.VerifyNoOtherCalls();
    }
}

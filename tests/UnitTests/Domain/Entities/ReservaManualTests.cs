using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

/// <summary>Manual-payment rules of <see cref="Reserva"/> (F04): expiry, confirm, cancel and extend.</summary>
public class ReservaManualTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;
    private static readonly TimeSpan Manual48h = TimeSpan.FromHours(48);

    private static Reserva Crear(MetodoPago metodo, EstadoReserva estado, DateTimeOffset? expiraEn = null)
    {
        var reserva = CatalogoFactory.Reserva(metodo: metodo, expiraEn: expiraEn ?? Now.AddHours(48));
        switch (estado)
        {
            case EstadoReserva.Pagada: reserva.Pagar(Now); break;
            case EstadoReserva.Expirada: reserva.Expirar(Now); break;
            case EstadoReserva.Cancelada: reserva.Cancelar(Now); break;
        }

        return reserva;
    }

    [Fact]
    public void CalcularCaducidadManual_FarClosing_UsesTheLifetime()
    {
        Reserva.CalcularCaducidadManual(Now, Manual48h, Now.AddHours(100)).Should().Be(Now.AddHours(48));
    }

    [Fact]
    public void CalcularCaducidadManual_CloseClosing_ClipsToTheClosingTime()
    {
        Reserva.CalcularCaducidadManual(Now, Manual48h, Now.AddHours(10)).Should().Be(Now.AddHours(10));
    }

    [Theory]
    [InlineData(119)]
    [InlineData(0)]
    [InlineData(-60)]
    public void CalcularCaducidadManual_LessThanTwoHoursToClosing_ThrowsEdicionNoReservable(int minutos)
    {
        var act = () => Reserva.CalcularCaducidadManual(Now, Manual48h, Now.AddMinutes(minutos));

        var ex = act.Should().Throw<BusinessRuleException>().Which;
        ex.Code.Should().Be(ErrorCodes.EdicionNoReservable);
        ex.Message.Should().Be("Para esta fecha solo se puede pagar con tarjeta");
    }

    [Fact]
    public void CalcularCaducidadManual_ExactlyTwoHours_IsAllowed()
    {
        Reserva.CalcularCaducidadManual(Now, Manual48h, Now.AddHours(2)).Should().Be(Now.AddHours(2));
    }

    // ---- Confirm ----

    [Fact]
    public void ConfirmarPagoManual_PendingManual_PaysAndAddsNote()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente);

        reserva.ConfirmarPagoManual(Now.AddHours(1), hayPlaza: false, nota: "Bizum 12:30").Should().BeTrue();

        reserva.Estado.Should().Be(EstadoReserva.Pagada);
        reserva.PagadaEn.Should().Be(Now.AddHours(1));
        reserva.NotaInterna.Should().Be("Bizum 12:30");
    }

    [Fact]
    public void ConfirmarPagoManual_ExpiredManualWithSeat_Pays()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Expirada);

        reserva.ConfirmarPagoManual(Now, hayPlaza: true, nota: null).Should().BeTrue();

        reserva.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public void ConfirmarPagoManual_ExpiredManualWithoutSeat_ThrowsSinPlazas()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Expirada);

        var act = () => reserva.ConfirmarPagoManual(Now, hayPlaza: false, nota: null);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.SinPlazas);
        reserva.Estado.Should().Be(EstadoReserva.Expirada);
    }

    [Fact]
    public void ConfirmarPagoManual_PendingPastExpiryWithoutSeat_ThrowsSinPlazas()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente, expiraEn: Now.AddHours(1));

        var act = () => reserva.ConfirmarPagoManual(Now.AddHours(2), hayPlaza: false, nota: null);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.SinPlazas);
    }

    [Fact]
    public void ConfirmarPagoManual_Paid_IsIdempotent()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pagada);
        var pagadaEn = reserva.PagadaEn;

        reserva.ConfirmarPagoManual(Now.AddDays(1), hayPlaza: false, nota: "otra").Should().BeFalse();

        reserva.PagadaEn.Should().Be(pagadaEn);
        reserva.NotaInterna.Should().BeNull();
    }

    [Theory]
    [InlineData(MetodoPago.Stripe, EstadoReserva.Pendiente)]
    [InlineData(MetodoPago.Stripe, EstadoReserva.Expirada)]
    [InlineData(MetodoPago.Manual, EstadoReserva.Cancelada)]
    [InlineData(MetodoPago.Stripe, EstadoReserva.Cancelada)]
    public void ConfirmarPagoManual_InvalidCombinations_ThrowEstadoInvalido(MetodoPago metodo, EstadoReserva estado)
    {
        var reserva = Crear(metodo, estado);

        var act = () => reserva.ConfirmarPagoManual(Now, hayPlaza: true, nota: null);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        reserva.Estado.Should().Be(estado);
    }

    [Fact]
    public void ConfirmarPagoManual_CardBooking_SaysCardPaymentsConfirmThemselves()
    {
        var act = () => Crear(MetodoPago.Stripe, EstadoReserva.Pendiente).ConfirmarPagoManual(Now, true, null);

        act.Should().Throw<BusinessRuleException>().WithMessage("Los pagos con tarjeta se confirman solos");
    }

    // ---- Cancel ----

    [Theory]
    [InlineData(MetodoPago.Manual, EstadoReserva.Pendiente)]
    [InlineData(MetodoPago.Stripe, EstadoReserva.Pendiente)]
    [InlineData(MetodoPago.Manual, EstadoReserva.Expirada)]
    public void CancelarDesdeBackoffice_PendingOrExpired_Cancels(MetodoPago metodo, EstadoReserva estado)
    {
        var reserva = Crear(metodo, estado);

        reserva.CancelarDesdeBackoffice(Now.AddHours(1), confirmarPagada: false, motivo: "No contesta").Should().BeTrue();

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.CanceladaEn.Should().Be(Now.AddHours(1));
        reserva.NotaInterna.Should().Be("No contesta");
    }

    [Fact]
    public void CancelarDesdeBackoffice_PaidWithoutConfirmation_ThrowsEstadoInvalido()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pagada);

        var act = () => reserva.CancelarDesdeBackoffice(Now, confirmarPagada: false, motivo: null);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        reserva.Estado.Should().Be(EstadoReserva.Pagada);
    }

    [Fact]
    public void CancelarDesdeBackoffice_PaidWithConfirmation_Cancels()
    {
        var reserva = Crear(MetodoPago.Stripe, EstadoReserva.Pagada);

        reserva.CancelarDesdeBackoffice(Now, confirmarPagada: true, motivo: null).Should().BeTrue();

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public void CancelarDesdeBackoffice_AlreadyCancelled_IsIdempotent()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Cancelada);

        reserva.CancelarDesdeBackoffice(Now.AddDays(1), confirmarPagada: false, motivo: null).Should().BeFalse();

        reserva.CanceladaEn.Should().Be(Now);
    }

    // ---- Extend ----

    [Fact]
    public void Ampliar_Pending_AddsHoursFromCurrentExpiry()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente, expiraEn: Now.AddHours(10));

        reserva.Ampliar(Now, 24, cierre: Now.AddHours(100), hayPlaza: false);

        reserva.ExpiraEn.Should().Be(Now.AddHours(34));
    }

    [Fact]
    public void Ampliar_ClipsToTheClosingTime()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente, expiraEn: Now.AddHours(10));

        reserva.Ampliar(Now, 168, cierre: Now.AddHours(100), hayPlaza: false);

        reserva.ExpiraEn.Should().Be(Now.AddHours(100));
    }

    [Fact]
    public void Ampliar_ExpiredWithSeat_ReactivatesFromNow()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Expirada, expiraEn: Now.AddHours(1));
        var later = Now.AddHours(5);

        reserva.Ampliar(later, 12, cierre: Now.AddHours(100), hayPlaza: true);

        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        reserva.ExpiradaEn.Should().BeNull();
        reserva.ExpiraEn.Should().Be(later.AddHours(12));
    }

    [Fact]
    public void Ampliar_ExpiredWithoutSeat_ThrowsSinPlazas()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Expirada);

        var act = () => reserva.Ampliar(Now, 12, Now.AddHours(100), hayPlaza: false);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.SinPlazas);
        reserva.Estado.Should().Be(EstadoReserva.Expirada);
    }

    [Fact]
    public void Ampliar_ClosingAlreadyReached_ThrowsEdicionNoReservable()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente);

        var act = () => reserva.Ampliar(Now, 12, cierre: Now, hayPlaza: true);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EdicionNoReservable);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)]
    public void Ampliar_HoursOutOfRange_ThrowsDomainException(int horas)
    {
        var act = () => Crear(MetodoPago.Manual, EstadoReserva.Pendiente).Ampliar(Now, horas, Now.AddHours(500), true);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(MetodoPago.Stripe, EstadoReserva.Pendiente)]
    [InlineData(MetodoPago.Manual, EstadoReserva.Pagada)]
    [InlineData(MetodoPago.Manual, EstadoReserva.Cancelada)]
    public void Ampliar_InvalidCombinations_ThrowEstadoInvalido(MetodoPago metodo, EstadoReserva estado)
    {
        var act = () => Crear(metodo, estado).Ampliar(Now, 12, Now.AddHours(100), hayPlaza: true);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
    }

    [Fact]
    public void Notes_AreAppendedAndTruncatedTo500()
    {
        var reserva = Crear(MetodoPago.Manual, EstadoReserva.Pendiente);

        reserva.CancelarDesdeBackoffice(Now, false, new string('a', 499));
        reserva.NotaInterna!.Length.Should().Be(499);

        var otra = Crear(MetodoPago.Manual, EstadoReserva.Expirada);
        otra.ConfirmarPagoManual(Now, true, "primera");
        otra.CancelarDesdeBackoffice(Now, true, "segunda");
        otra.NotaInterna.Should().Be("primera\nsegunda");

        var larga = Crear(MetodoPago.Manual, EstadoReserva.Pendiente);
        larga.ConfirmarPagoManual(Now, false, new string('x', 400));
        larga.CancelarDesdeBackoffice(Now, true, new string('y', 400));
        larga.NotaInterna!.Length.Should().Be(Reserva.NotaInternaMaxLength);
    }
}

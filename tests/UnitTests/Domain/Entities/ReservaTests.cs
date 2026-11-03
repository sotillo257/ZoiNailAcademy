using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

public class ReservaTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;

    [Fact]
    public void Create_WithValidData_IsPendingAndCopiesPrice()
    {
        var edicion = CatalogoFactory.Edicion(CatalogoFactory.Curso(precio: 149.90m));

        var reserva = Reserva.Create(
            edicion, "CUR-ABC234", MetodoPago.Stripe, " Lucía ", " Lucia@Mail.COM ", "+34612345678", Now.AddMinutes(35), Now);

        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        reserva.Importe.Should().Be(149.90m);
        reserva.PrecioCurso.Should().Be(149.90m);
        reserva.EdicionId.Should().Be(edicion.Id);
        reserva.Nombre.Should().Be("Lucía");
        reserva.Email.Should().Be("lucia@mail.com");
        reserva.ConsentimientoEn.Should().Be(Now);
        reserva.OcupaPlaza(Now.AddMinutes(34)).Should().BeTrue();
        reserva.OcupaPlaza(Now.AddMinutes(35)).Should().BeFalse();
    }

    [Fact]
    public void Create_CourseWithSeatDeposit_ChargesTheDepositAndCopiesTheFullPrice()
    {
        var edicion = CatalogoFactory.Edicion(CatalogoFactory.Curso(precio: 149.90m, precioReserva: 40m));

        var reserva = CatalogoFactory.Reserva(edicion);

        (reserva.Importe, reserva.PrecioCurso).Should().Be((40m, 149.90m));
    }

    [Theory]
    [InlineData("CUR-ABC23", "Lucía", 10)]
    [InlineData("CUR-ABC234", " ", 10)]
    [InlineData("CUR-ABC234", "Lucía", 0)]
    public void Create_WithInvalidData_ThrowsDomainException(string codigo, string nombre, int minutos)
    {
        var act = () => Reserva.Create(
            CatalogoFactory.Edicion(), codigo, MetodoPago.Stripe, nombre, "a@b.com", "+34612345678", Now.AddMinutes(minutos), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void OcupaPlazaEn_MatchesInMemoryRule()
    {
        var pendiente = CatalogoFactory.Reserva();
        var pagada = CatalogoFactory.Reserva();
        pagada.Pagar(Now);
        var cancelada = CatalogoFactory.Reserva();
        cancelada.Cancelar(Now);
        var predicate = Reserva.OcupaPlazaEn(Now.AddMinutes(40)).Compile();

        predicate(pendiente).Should().BeFalse("it expired at +35 min");
        predicate(pagada).Should().BeTrue();
        predicate(cancelada).Should().BeFalse();
    }

    [Fact]
    public void Pagar_Pending_BecomesPaid()
    {
        var reserva = CatalogoFactory.Reserva();

        reserva.Pagar(Now.AddMinutes(1), "pi_123");

        reserva.Estado.Should().Be(EstadoReserva.Pagada);
        reserva.PagadaEn.Should().Be(Now.AddMinutes(1));
        reserva.StripePaymentIntentId.Should().Be("pi_123");
    }

    [Fact]
    public void Expirar_Pending_BecomesExpired()
    {
        var reserva = CatalogoFactory.Reserva();

        reserva.Expirar(Now.AddMinutes(40));

        reserva.Estado.Should().Be(EstadoReserva.Expirada);
        reserva.ExpiradaEn.Should().Be(Now.AddMinutes(40));
    }

    [Fact]
    public void Cancelar_Pending_BecomesCancelled()
    {
        var reserva = CatalogoFactory.Reserva();

        reserva.Cancelar(Now.AddMinutes(2));

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.CanceladaEn.Should().Be(Now.AddMinutes(2));
    }

    [Theory]
    [InlineData(EstadoReserva.Expirada)]
    [InlineData(EstadoReserva.Cancelada)]
    public void PagarTrasCaducidad_FromExpiredOrCancelled_BecomesPaid(EstadoReserva estado)
    {
        var reserva = EnEstado(estado);
        reserva.RegistrarPagoSinPlaza("pi_1");

        reserva.PagarTrasCaducidad(Now.AddHours(1), "pi_1");

        reserva.Estado.Should().Be(EstadoReserva.Pagada);
        reserva.RequiereRevision.Should().BeFalse();
    }

    [Theory]
    [InlineData(EstadoReserva.Expirada)]
    [InlineData(EstadoReserva.Cancelada)]
    public void RegistrarPagoSinPlaza_KeepsStateAndFlagsReview(EstadoReserva estado)
    {
        var reserva = EnEstado(estado);

        reserva.RegistrarPagoSinPlaza("pi_9");

        reserva.Estado.Should().Be(estado);
        reserva.RequiereRevision.Should().BeTrue();
        reserva.StripePaymentIntentId.Should().Be("pi_9");
    }

    [Fact]
    public void AsignarSesionStripe_OnlyForPendingCardBookings()
    {
        var tarjeta = CatalogoFactory.Reserva();
        tarjeta.AsignarSesionStripe("cs_1");
        tarjeta.StripeSessionId.Should().Be("cs_1");

        var manual = CatalogoFactory.Reserva(metodo: MetodoPago.Manual);
        ((Action)(() => manual.AsignarSesionStripe("cs_2"))).Should().Throw<BusinessRuleException>();
    }

    public static TheoryData<EstadoReserva, string> TransicionesInvalidas => new()
    {
        { EstadoReserva.Pagada, nameof(Reserva.Pagar) },
        { EstadoReserva.Expirada, nameof(Reserva.Pagar) },
        { EstadoReserva.Pagada, nameof(Reserva.Expirar) },
        { EstadoReserva.Cancelada, nameof(Reserva.Expirar) },
        { EstadoReserva.Pagada, nameof(Reserva.Cancelar) },
        { EstadoReserva.Expirada, nameof(Reserva.Cancelar) },
        { EstadoReserva.Pendiente, nameof(Reserva.PagarTrasCaducidad) },
        { EstadoReserva.Pagada, nameof(Reserva.PagarTrasCaducidad) },
        { EstadoReserva.Pendiente, nameof(Reserva.RegistrarPagoSinPlaza) },
    };

    [Theory]
    [MemberData(nameof(TransicionesInvalidas))]
    public void InvalidTransition_ThrowsEstadoInvalidoAndKeepsState(EstadoReserva estado, string transicion)
    {
        var reserva = EnEstado(estado);
        Action act = transicion switch
        {
            nameof(Reserva.Pagar) => () => reserva.Pagar(Now),
            nameof(Reserva.Expirar) => () => reserva.Expirar(Now),
            nameof(Reserva.Cancelar) => () => reserva.Cancelar(Now),
            nameof(Reserva.PagarTrasCaducidad) => () => reserva.PagarTrasCaducidad(Now),
            _ => () => reserva.RegistrarPagoSinPlaza()
        };

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be(ErrorCodes.EstadoInvalido);
        reserva.Estado.Should().Be(estado);
    }

    private static Reserva EnEstado(EstadoReserva estado)
    {
        var reserva = CatalogoFactory.Reserva();
        switch (estado)
        {
            case EstadoReserva.Pagada: reserva.Pagar(Now); break;
            case EstadoReserva.Expirada: reserva.Expirar(Now); break;
            case EstadoReserva.Cancelada: reserva.Cancelar(Now); break;
        }

        return reserva;
    }

    [Fact]
    public void Create_StoresNameEmailAndPhoneNormalizedForSearch()
    {
        var reserva = Reserva.Create(
            CatalogoFactory.Edicion(), "CUR-ABC234", MetodoPago.Manual, " José Núñez ", "Jose@Mail.COM", "+34612345678",
            Now.AddHours(48), Now);

        reserva.Busqueda.Should().Be("jose nunez jose@mail.com +34612345678");
    }

    [Fact]
    public void MarcarRevisada_PaymentWithoutSeat_ClearsTheMarkAndAppendsTheNote()
    {
        var reserva = CatalogoFactory.Reserva();
        reserva.Expirar(Now);
        reserva.RegistrarPagoSinPlaza("pi_123");

        reserva.MarcarRevisada(" Reembolsado el 05/12 ");

        reserva.RequiereRevision.Should().BeFalse();
        reserva.NotaInterna.Should().Be("Reembolsado el 05/12");
        reserva.Estado.Should().Be(EstadoReserva.Expirada);
    }

    [Fact]
    public void MarcarRevisada_WithoutTheMark_ChangesNothing()
    {
        var reserva = CatalogoFactory.Reserva();

        reserva.MarcarRevisada("Nota");

        reserva.RequiereRevision.Should().BeFalse();
        reserva.NotaInterna.Should().BeNull();
    }
}

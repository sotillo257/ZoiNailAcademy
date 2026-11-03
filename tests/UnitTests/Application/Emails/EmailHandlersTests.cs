using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Contacto.EnviarMensajeContacto;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.Emails.EnviarEmailsPendientes;
using ZoiNailAcademy.Application.Emails.NotificarEventos;
using ZoiNailAcademy.Application.Emails.PrevisualizarEmail;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Emails;

public class EmailEventHandlersTests
{
    private readonly CatalogoTestContext _ctx = new();

    private Reserva ReservaEnBd(MetodoPago metodo = MetodoPago.Stripe)
    {
        var reserva = CatalogoFactory.Reserva(metodo: metodo, email: "lucia@mail.com");
        _ctx.SetupReservas(reserva);
        return reserva;
    }

    [Fact]
    public async Task ReservaPagada_EnqueuesBuyerAndClientEmailsWithFreeSeats()
    {
        var reserva = ReservaEnBd();
        reserva.Pagar(CatalogoFactory.Now);
        _ctx.Ocupaciones[reserva.EdicionId] = new Ocupacion(7, 1);
        var handler = new ReservaPagadaEmailsHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), _ctx.OcupacionReader.Object, NullLogger<ReservaPagadaEmailsHandler>.Instance);

        await handler.Handle(new ReservaPagadaEvent(reserva.Id), CancellationToken.None);

        _ctx.Emails.Select(e => (e.Tipo, e.Para, e.ReplyTo, e.ReferenciaId)).Should().BeEquivalentTo(
        [
            (TiposEmail.ReservaPagadaComprador, "lucia@mail.com", "zoi@academia.test", (Guid?)reserva.Id),
            (TiposEmail.ReservaPagadaClienta, "avisos@academia.test", (string?)null, (Guid?)reserva.Id)
        ]);
        _ctx.Emails.Single(e => e.Tipo == TiposEmail.ReservaPagadaClienta).Texto.Should().Contain("Plazas libres: 2");
    }

    [Fact]
    public async Task ReservaPagada_AlreadyEnqueued_DoesNotDuplicate()
    {
        var reserva = ReservaEnBd();
        reserva.Pagar(CatalogoFactory.Now);
        var handler = new ReservaPagadaEmailsHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), _ctx.OcupacionReader.Object, NullLogger<ReservaPagadaEmailsHandler>.Instance);

        await handler.Handle(new ReservaPagadaEvent(reserva.Id), CancellationToken.None);
        _ctx.SetupEmails([.. _ctx.Emails]);
        await handler.Handle(new ReservaPagadaEvent(reserva.Id), CancellationToken.None);

        _ctx.Emails.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReservaPagada_WithoutClientAddress_SkipsTheClientNotice()
    {
        _ctx.EmailSettings.Setup(x => x.AvisosClienta).Returns((string?)null);
        var reserva = ReservaEnBd();
        var handler = new ReservaPagadaEmailsHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), _ctx.OcupacionReader.Object, NullLogger<ReservaPagadaEmailsHandler>.Instance);

        await handler.Handle(new ReservaPagadaEvent(reserva.Id), CancellationToken.None);

        _ctx.Emails.Should().ContainSingle().Which.Tipo.Should().Be(TiposEmail.ReservaPagadaComprador);
    }

    [Theory]
    [InlineData(MetodoPago.Stripe, 0)]
    [InlineData(MetodoPago.Manual, 1)]
    public async Task ReservaExpirada_OnlyManualBuyersAreEmailed(MetodoPago metodo, int esperados)
    {
        var reserva = ReservaEnBd(metodo);
        var handler = new ReservaExpiradaEmailHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), NullLogger<ReservaExpiradaEmailHandler>.Instance);

        await handler.Handle(new ReservaExpiradaEvent(reserva.Id), CancellationToken.None);

        _ctx.Emails.Should().HaveCount(esperados);
        _ctx.Emails.Should().NotContain(e => e.Tipo != TiposEmail.ReservaExpiradaComprador);
    }

    [Fact]
    public async Task ReservaManualCreada_SendsPaymentDetailsToTheBuyerAndANoticeToTheClient()
    {
        var reserva = ReservaEnBd(MetodoPago.Manual);
        var handler = new ReservaManualCreadaEmailsHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), _ctx.CreateDtoFactory(), NullLogger<ReservaManualCreadaEmailsHandler>.Instance);

        await handler.Handle(new ReservaManualCreadaEvent(reserva.Id), CancellationToken.None);

        _ctx.Emails.Select(e => e.Tipo).Should().BeEquivalentTo([TiposEmail.ReservaManualComprador, TiposEmail.ReservaManualClienta]);
        _ctx.Emails.Single(e => e.Tipo == TiposEmail.ReservaManualComprador).Html
            .Should().Contain("ES12 3456").And.Contain("https://wa.me/34600111222?text=");
    }

    [Fact]
    public async Task PagoSinPlaza_NotifiesTheClientWithThePaymentId()
    {
        var reserva = ReservaEnBd();
        reserva.Expirar(CatalogoFactory.Now);
        reserva.RegistrarPagoSinPlaza("pi_999");
        var handler = new PagoSinPlazaEmailHandler(_ctx.DbContext.Object, _ctx.CreateBandeja(), NullLogger<PagoSinPlazaEmailHandler>.Instance);

        await handler.Handle(new PagoTrasCaducidadSinPlazaEvent(reserva.Id), CancellationToken.None);

        var email = _ctx.Emails.Should().ContainSingle().Subject;
        (email.Tipo, email.Para).Should().Be((TiposEmail.PagoSinPlazaClienta, "avisos@academia.test"));
        email.Texto.Should().Contain("pi_999");
    }

    [Fact]
    public async Task UnknownBooking_EnqueuesNothing()
    {
        var handler = new ReservaPagadaEmailsHandler(
            _ctx.DbContext.Object, _ctx.CreateBandeja(), _ctx.OcupacionReader.Object, NullLogger<ReservaPagadaEmailsHandler>.Instance);

        await handler.Handle(new ReservaPagadaEvent(Guid.NewGuid()), CancellationToken.None);

        _ctx.Emails.Should().BeEmpty();
    }
}

public class EnviarMensajeContactoTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly EnviarMensajeContactoValidator _validator = new();

    private EnviarMensajeContactoHandler Handler() =>
        new(_ctx.CreateBandeja(), _ctx.EmailSettings.Object, NullLogger<EnviarMensajeContactoHandler>.Instance);

    private static EnviarMensajeContactoCommand Valid() =>
        new("Ana", " Ana@Mail.com ", "612 34 56 78", "¿Hay plazas en marzo?", true, null);

    [Fact]
    public async Task Handle_EnqueuesForTheClientWithReplyToTheSender()
    {
        await Handler().Handle(Valid(), CancellationToken.None);

        var email = _ctx.Emails.Should().ContainSingle().Subject;
        (email.Tipo, email.Para, email.ReplyTo, email.ReferenciaId)
            .Should().Be((TiposEmail.ContactoClienta, "avisos@academia.test", "ana@mail.com", (Guid?)null));
        email.Texto.Should().Contain("+34612345678").And.Contain("¿Hay plazas en marzo?");
    }

    [Fact]
    public async Task Handle_WithAcknowledgementEnabled_AlsoEmailsTheSender()
    {
        _ctx.EmailSettings.Setup(x => x.AcuseContacto).Returns(true);

        await Handler().Handle(Valid(), CancellationToken.None);

        _ctx.Emails.Should().Contain(e => e.Tipo == TiposEmail.ContactoAcuse && e.Para == "ana@mail.com");
    }

    [Fact]
    public async Task Handle_Honeypot_EnqueuesNothing()
    {
        await Handler().Handle(Valid() with { Website = "http://spam" }, CancellationToken.None);

        _ctx.Emails.Should().BeEmpty();
    }

    [Fact]
    public void Validate_OptionalPhoneAndMessageLength()
    {
        _validator.Validate(Valid() with { Telefono = null }).IsValid.Should().BeTrue();
        _validator.Validate(Valid() with { Telefono = "123" }).Errors.Should().ContainSingle(e => e.PropertyName == "Telefono");
        _validator.Validate(Valid() with { Mensaje = "Hola" }).Errors.Should().ContainSingle(e => e.PropertyName == "Mensaje");
        _validator.Validate(Valid() with { Mensaje = new string('a', 3001) }).Errors.Should().ContainSingle(e => e.PropertyName == "Mensaje");
        _validator.Validate(Valid() with { AceptaPrivacidad = false, Email = "x" }).Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo(["AceptaPrivacidad", "Email"]);
    }
}

public class EnviarEmailsPendientesHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly Mock<IEmailSender> _sender = new();

    private EnviarEmailsPendientesHandler Handler() =>
        new(_ctx.DbContext.Object, _sender.Object, _ctx.TimeProvider, NullLogger<EnviarEmailsPendientesHandler>.Instance);

    private static EmailSaliente Pendiente(DateTimeOffset? cuando = null) => EmailSaliente.Create(
        "a@mail.com", null, "Asunto", "<p>x</p>", "x", "Tipo", Guid.NewGuid(), cuando ?? CatalogoFactory.Now);

    [Fact]
    public async Task Handle_SendsDueEmailsAndMarksThemSent()
    {
        var due = Pendiente();
        var futuro = Pendiente(CatalogoFactory.Now.AddMinutes(5));
        _ctx.SetupEmails(due, futuro);
        _sender.Setup(x => x.EnviarAsync(It.IsAny<EmailMensaje>(), It.IsAny<CancellationToken>())).ReturnsAsync("re_1");

        var result = await Handler().Handle(new EnviarEmailsPendientesCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        (due.Estado, due.ProveedorId).Should().Be((EstadoEmail.Enviado, "re_1"));
        futuro.Estado.Should().Be(EstadoEmail.Pendiente);
        _sender.Verify(x => x.EnviarAsync(It.Is<EmailMensaje>(m => m.Id == due.Id), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ProviderFails_SchedulesARetryAndContinuesWithTheNext()
    {
        var primero = Pendiente(CatalogoFactory.Now.AddSeconds(-2));
        var segundo = Pendiente(CatalogoFactory.Now.AddSeconds(-1));
        _ctx.SetupEmails(primero, segundo);
        _sender.Setup(x => x.EnviarAsync(It.Is<EmailMensaje>(m => m.Id == primero.Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Resend respondió 429"));
        _sender.Setup(x => x.EnviarAsync(It.Is<EmailMensaje>(m => m.Id == segundo.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync("re_2");

        var result = await Handler().Handle(new EnviarEmailsPendientesCommand(), CancellationToken.None);

        result.Value.Should().Be(1);
        (primero.Estado, primero.Intentos, primero.UltimoError).Should().Be((EstadoEmail.Pendiente, 1, "Resend respondió 429"));
        primero.ProximoIntentoEn.Should().Be(CatalogoFactory.Now.AddMinutes(1));
        segundo.Estado.Should().Be(EstadoEmail.Enviado);
    }

    [Fact]
    public async Task Handle_LockHeldByAnotherReplica_SendsNothing()
    {
        _ctx.SetupEmails(Pendiente());
        _ctx.DbContext.Setup(x => x.TryAdquirirLockAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var result = await Handler().Handle(new EnviarEmailsPendientesCommand(), CancellationToken.None);

        result.Value.Should().Be(0);
        _sender.VerifyNoOtherCalls();
    }
}

public class PrevisualizarEmailHandlerTests
{
    private readonly CatalogoTestContext _ctx = new();

    [Fact]
    public async Task Handle_EveryTypeRendersAndAnUnknownOneIsNotFound()
    {
        var handler = new PrevisualizarEmailHandler(_ctx.CreateBandeja(), _ctx.CreateDtoFactory(), _ctx.TimeProvider);

        foreach (var tipo in TiposEmail.Todos)
        {
            var contenido = (await handler.Handle(new PrevisualizarEmailQuery(tipo), CancellationToken.None)).Value;
            contenido.Html.Should().StartWith("<!DOCTYPE html>", tipo);
            contenido.Asunto.Should().NotBeNullOrWhiteSpace();
        }

        var act = () => handler.Handle(new PrevisualizarEmailQuery("NoExiste"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

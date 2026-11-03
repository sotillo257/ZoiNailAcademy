using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Reservas;

public class CrearReservaServiceTests
{
    private readonly CatalogoTestContext _ctx = new();
    private readonly CrearReservaService _service;
    private readonly Edicion _edicion = CatalogoFactory.Edicion(plazas: 2);

    public CrearReservaServiceTests()
    {
        _ctx.SetupEdiciones(_edicion);
        _service = new CrearReservaService(_ctx.DbContext.Object, _ctx.OcupacionReader.Object, _ctx.Settings.Object, _ctx.TimeProvider);
    }

    private NuevaReserva Nueva(string email = "Nueva@Mail.com", string telefono = "612 34 56 78") =>
        new(_edicion.Id, "Lucía", email, telefono, MetodoPago.Stripe, (_, now) => now.AddMinutes(35));

    [Fact]
    public async Task CrearAsync_WithFreeSeat_LocksInsertsNormalizedAndCommits()
    {
        var reserva = await _service.CrearAsync(Nueva(), CancellationToken.None);

        _ctx.Reservas.Should().ContainSingle().Which.Should().BeSameAs(reserva);
        reserva.Email.Should().Be("nueva@mail.com");
        reserva.Telefono.Should().Be("+34612345678");
        reserva.Importe.Should().Be(_edicion.Curso.Precio);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        _ctx.DbContext.Verify(x => x.BloquearEdicionAsync(_edicion.Id, It.IsAny<CancellationToken>()), Times.Once);
        _ctx.VerifyCommitted(Times.Once());
    }

    [Fact]
    public async Task CrearAsync_NoFreeSeats_ThrowsSinPlazasAndDoesNotCommit()
    {
        _ctx.Ocupaciones[_edicion.Id] = new Ocupacion(1, 1);

        var act = () => _service.CrearAsync(Nueva(), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.SinPlazas);
        _ctx.Reservas.Should().BeEmpty();
        _ctx.VerifyCommitted(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_EditionNotBookable_ThrowsEdicionNoReservable()
    {
        var dentroDelCierre = CatalogoFactory.Edicion(inicio: CatalogoFactory.Now.AddHours(10));
        _ctx.SetupEdiciones(dentroDelCierre);

        var act = () => _service.CrearAsync(Nueva() with { EdicionId = dentroDelCierre.Id }, CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(ErrorCodes.EdicionNoReservable);
        _ctx.VerifyCommitted(Times.Never());
    }

    [Fact]
    public async Task CrearAsync_UnknownEdition_ThrowsNotFound()
    {
        var act = () => _service.CrearAsync(Nueva() with { EdicionId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CrearAsync_ContactAtPendingLimitBySamePhone_ThrowsLimitePendientes()
    {
        var otra = CatalogoFactory.Edicion();
        _ctx.SetupReservas(
            CatalogoFactory.Reserva(otra, email: "a@mail.com", telefono: "+34612345678"),
            CatalogoFactory.Reserva(otra, email: "b@mail.com", telefono: "+34612345678"));

        var act = () => _service.CrearAsync(Nueva(email: "c@mail.com"), CancellationToken.None);

        (await act.Should().ThrowAsync<TooManyRequestsException>()).Which.Code.Should().Be(ErrorCodes.LimitePendientes);
    }

    [Fact]
    public async Task CrearAsync_ExpiredOrPaidBookingsOfTheContact_DoNotCountForTheLimit()
    {
        var caducada = CatalogoFactory.Reserva(email: "nueva@mail.com", expiraEn: CatalogoFactory.Now.AddMinutes(1));
        _ctx.TimeProvider.Advance(TimeSpan.FromMinutes(2));
        var pagada = CatalogoFactory.Reserva(email: "nueva@mail.com", expiraEn: CatalogoFactory.Now.AddHours(1));
        pagada.Pagar(CatalogoFactory.Now);
        _ctx.SetupReservas(caducada, pagada);

        var reserva = await _service.CrearAsync(Nueva() with { CalcularExpiraEn = (_, now) => now.AddHours(1) }, CancellationToken.None);

        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task CrearAsync_InvalidPhone_ThrowsValidationException()
    {
        var act = () => _service.CrearAsync(Nueva(telefono: "123"), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("Telefono");
    }
}

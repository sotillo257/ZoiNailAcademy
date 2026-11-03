using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Time.Testing;
using MockQueryable.Moq;
using Moq;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.TestSupport;

/// <summary>Mocks shared by the course and edition handler tests.</summary>
public class CatalogoTestContext
{
    public Mock<IAppDbContext> DbContext { get; } = new();
    public Mock<IOcupacionReader> OcupacionReader { get; } = new();
    public Mock<IReservasSettings> Settings { get; } = new();
    public Mock<IPublisher> Publisher { get; } = new();
    public FakeTimeProvider TimeProvider { get; } = new(CatalogoFactory.Now);

    public List<Curso> Cursos { get; private set; } = [];
    public List<Edicion> Ediciones { get; private set; } = [];
    public Dictionary<Guid, Ocupacion> Ocupaciones { get; } = [];
    public List<Reserva> Reservas { get; private set; } = [];
    public List<StripeEvento> StripeEventos { get; private set; } = [];
    public Mock<IDbContextTransaction> Transaction { get; } = new();
    public Mock<IAppSettings> AppSettings { get; } = new();

    public CatalogoTestContext()
    {
        Settings.Setup(x => x.CierreAntesDeInicio).Returns(TimeSpan.FromHours(24));
        Settings.Setup(x => x.UmbralUltimasPlazas).Returns(3);
        OcupacionReader
            .Setup(x => x.LeerAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Ocupaciones);
        Settings.Setup(x => x.StripeBloqueo).Returns(TimeSpan.FromMinutes(30));
        Settings.Setup(x => x.MaxPendientesPorContacto).Returns(2);
        AppSettings.Setup(x => x.BaseUrl).Returns("https://web.test");
        AppSettings.Setup(x => x.TimeZone).Returns(TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"));
        DbContext.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Transaction.Object);
        SetupCursos();
        SetupEdiciones();
        SetupReservas();
        SetupStripeEventos();
    }

    public void SetupReservas(params Reserva[] reservas)
    {
        Reservas = reservas.ToList();
        var mockSet = Reservas.BuildMockDbSet();
        mockSet.Setup(x => x.Add(It.IsAny<Reserva>())).Callback<Reserva>(Reservas.Add);
        DbContext.Setup(x => x.Reservas).Returns(mockSet.Object);
    }

    public void SetupStripeEventos(params StripeEvento[] eventos)
    {
        StripeEventos = eventos.ToList();
        var mockSet = StripeEventos.BuildMockDbSet();
        mockSet.Setup(x => x.Add(It.IsAny<StripeEvento>())).Callback<StripeEvento>(StripeEventos.Add);
        DbContext.Setup(x => x.StripeEventos).Returns(mockSet.Object);
    }

    public void VerifyCommitted(Times times) =>
        Transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), times);

    /// <summary>Post-commit events are published typed as <see cref="INotification"/> (MediatR dispatches on the runtime type).</summary>
    public void VerifyPublished<TEvent>(Func<TEvent, bool> match, Times times) where TEvent : INotification =>
        Publisher.Verify(
            x => x.Publish(It.Is<INotification>(e => e is TEvent && match((TEvent)e)), It.IsAny<CancellationToken>()), times);

    public EdicionesPublicas CreateEdicionesPublicas() =>
        new(DbContext.Object, OcupacionReader.Object, Settings.Object, TimeProvider);

    public void SetupCursos(params Curso[] cursos)
    {
        Cursos = cursos.ToList();
        var mockSet = Cursos.BuildMockDbSet();
        mockSet.Setup(x => x.Add(It.IsAny<Curso>())).Callback<Curso>(Cursos.Add);
        DbContext.Setup(x => x.Cursos).Returns(mockSet.Object);
    }

    public void SetupEdiciones(params Edicion[] ediciones)
    {
        Ediciones = ediciones.ToList();
        var mockSet = Ediciones.BuildMockDbSet();
        mockSet.Setup(x => x.Add(It.IsAny<Edicion>())).Callback<Edicion>(Ediciones.Add);
        mockSet.Setup(x => x.Remove(It.IsAny<Edicion>())).Callback<Edicion>(e => Ediciones.Remove(e));
        DbContext.Setup(x => x.Ediciones).Returns(mockSet.Object);
    }

    public void VerifyPublished(Guid edicionId, Times times) =>
        VerifyPublished<EdicionPublicadaEvent>(e => e.EdicionId == edicionId, times);

    /// <summary>No Publish call at all, whatever its generic type argument.</summary>
    public void VerifyNothingPublished() => Publisher.Invocations.Should().BeEmpty();
}

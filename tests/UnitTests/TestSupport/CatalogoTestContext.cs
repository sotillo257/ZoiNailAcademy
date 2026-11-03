using MediatR;
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

    public CatalogoTestContext()
    {
        Settings.Setup(x => x.CierreAntesDeInicio).Returns(TimeSpan.FromHours(24));
        Settings.Setup(x => x.UmbralUltimasPlazas).Returns(3);
        OcupacionReader
            .Setup(x => x.LeerAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Ocupaciones);
        SetupCursos();
        SetupEdiciones();
    }

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
        Publisher.Verify(
            x => x.Publish(It.Is<EdicionPublicadaEvent>(e => e.EdicionId == edicionId), It.IsAny<CancellationToken>()),
            times);

    public void VerifyNothingPublished() =>
        Publisher.Verify(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
}

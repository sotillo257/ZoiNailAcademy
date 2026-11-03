using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class AvisosTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;

    [Fact]
    public void AvisoEnviado_Create_StoresThePairAndTheMomentInUtc()
    {
        var listaEsperaId = Guid.NewGuid();
        var edicionId = Guid.NewGuid();

        var aviso = AvisoEnviado.Create(listaEsperaId, edicionId, Now.ToOffset(TimeSpan.FromHours(1)));

        (aviso.ListaEsperaId, aviso.EdicionId, aviso.EncoladoEn).Should().Be((listaEsperaId, edicionId, Now));
        aviso.EncoladoEn.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AvisoPendiente_KeepsTheFirstMomentAndCountsPublications()
    {
        var cursoId = Guid.NewGuid();

        var pendiente = AvisoPendiente.Create(cursoId, Now);
        pendiente.RegistrarPublicacion();
        pendiente.RegistrarPublicacion();

        (pendiente.CursoId, pendiente.PendienteDesde, pendiente.Publicaciones).Should().Be((cursoId, Now, 3));
    }
}

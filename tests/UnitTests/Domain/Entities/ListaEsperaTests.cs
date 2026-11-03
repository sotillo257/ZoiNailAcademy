using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class ListaEsperaTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;

    private static ListaEspera Entrada(Curso? curso = null, Edicion? edicion = null) => ListaEspera.Create(
        curso ?? CatalogoFactory.Curso(), edicion, " Sábados de marzo ", " José Núñez ", " Jose@Mail.COM ", "+34612345678",
        "Texto aceptado", Now);

    [Fact]
    public void Create_WithValidData_NormalizesAndStoresTheConsent()
    {
        var curso = CatalogoFactory.Curso();
        var edicion = CatalogoFactory.Edicion(curso);

        var entrada = Entrada(curso, edicion);

        entrada.Id.Should().NotBeEmpty();
        entrada.CursoId.Should().Be(curso.Id);
        entrada.EdicionPreferidaId.Should().Be(edicion.Id);
        entrada.FechaPreferida.Should().Be("Sábados de marzo");
        entrada.Nombre.Should().Be("José Núñez");
        entrada.Email.Should().Be("jose@mail.com");
        entrada.ConsentimientoEn.Should().Be(Now);
        entrada.TextoConsentimiento.Should().Be("Texto aceptado");
        entrada.Contactado.Should().BeFalse();
        entrada.BajaEn.Should().BeNull();
        entrada.Busqueda.Should().Be("jose nunez jose@mail.com +34612345678");
    }

    [Fact]
    public void Create_GeneratesAUniqueUrlSafeUnsubscribeToken()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => Entrada().BajaToken).ToList();

        tokens.Should().OnlyHaveUniqueItems();
        tokens.Should().AllSatisfy(t =>
        {
            t.Length.Should().Be(43); // 32 bytes in base64url without padding
            t.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        });
    }

    [Fact]
    public void Create_EditionOfAnotherCourse_Throws()
    {
        var otraEdicion = CatalogoFactory.Edicion(CatalogoFactory.Curso("otro"));

        var act = () => Entrada(CatalogoFactory.Curso(), otraEdicion);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("", "a@mail.com", "+34612345678", "Texto")]
    [InlineData("Ana", " ", "+34612345678", "Texto")]
    [InlineData("Ana", "a@mail.com", "", "Texto")]
    [InlineData("Ana", "a@mail.com", "+34612345678", " ")]
    public void Create_MissingRequiredField_Throws(string nombre, string email, string telefono, string texto)
    {
        var act = () => ListaEspera.Create(CatalogoFactory.Curso(), null, null, nombre, email, telefono, texto, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ActualizarAlta_RefreshesDataAndResetsContactAndUnsubscribe()
    {
        var entrada = Entrada();
        entrada.MarcarContactado(true, Now);
        entrada.CambiarNotas("Llamada el lunes");
        var token = entrada.BajaToken;

        entrada.ActualizarAlta(null, null, "José N.", "+34698765432", "Texto nuevo", Now.AddDays(3));

        entrada.Nombre.Should().Be("José N.");
        entrada.Telefono.Should().Be("+34698765432");
        entrada.FechaPreferida.Should().BeNull();
        entrada.EdicionPreferidaId.Should().BeNull();
        entrada.TextoConsentimiento.Should().Be("Texto nuevo");
        entrada.ConsentimientoEn.Should().Be(Now.AddDays(3));
        entrada.Contactado.Should().BeFalse();
        entrada.ContactadoEn.Should().BeNull();
        entrada.BajaEn.Should().BeNull();
        entrada.Notas.Should().Be("Llamada el lunes");
        entrada.BajaToken.Should().Be(token);
        entrada.Busqueda.Should().Contain("+34698765432");
    }

    [Fact]
    public void MarcarContactado_SetsAndClearsTheMomentAndKeepsTheFirstOne()
    {
        var entrada = Entrada();

        entrada.MarcarContactado(true, Now);
        entrada.MarcarContactado(true, Now.AddHours(1));
        entrada.ContactadoEn.Should().Be(Now);
        entrada.Contactado.Should().BeTrue();

        entrada.MarcarContactado(false, Now.AddHours(2));
        entrada.Contactado.Should().BeFalse();
        entrada.ContactadoEn.Should().BeNull();
    }

    [Fact]
    public void CambiarNotas_TrimsClearsAndRejectsTooLong()
    {
        var entrada = Entrada();

        entrada.CambiarNotas("  Prefiere WhatsApp ");
        entrada.Notas.Should().Be("Prefiere WhatsApp");
        entrada.CambiarNotas("  ");
        entrada.Notas.Should().BeNull();

        var act = () => entrada.CambiarNotas(new string('a', ListaEspera.NotasMaxLength + 1));
        act.Should().Throw<DomainException>();
    }
}

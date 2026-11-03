using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

public class EmailSalienteTests
{
    private static readonly DateTimeOffset Now = CatalogoFactory.Now;

    private static EmailSaliente Email() =>
        EmailSaliente.Create("lucia@mail.com", null, "Asunto", "<p>Hola</p>", "Hola", "ReservaPagada.Comprador", Guid.NewGuid(), Now);

    [Fact]
    public void Create_IsPendingAndDueNow()
    {
        var email = Email();

        email.Estado.Should().Be(EstadoEmail.Pendiente);
        email.Intentos.Should().Be(0);
        email.ProximoIntentoEn.Should().Be(Now);
    }

    [Fact]
    public void Create_TruncatesTheSubjectAndRejectsMissingFields()
    {
        EmailSaliente.Create("a@b.c", null, new string('a', 400), "h", "t", "Tipo", null, Now)
            .Asunto.Should().HaveLength(EmailSaliente.AsuntoMaxLength);

        var act = () => EmailSaliente.Create(" ", null, "Asunto", "h", "t", "Tipo", null, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RegistrarFallo_AppliesTheBackoffAndFailsAfterFiveAttempts()
    {
        var email = Email();
        var esperas = new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1) };

        for (var i = 0; i < esperas.Length; i++)
        {
            email.RegistrarFallo("429 Too Many Requests", Now).Should().BeFalse();
            email.Intentos.Should().Be(i + 1);
            email.ProximoIntentoEn.Should().Be(Now + esperas[i]);
            email.Estado.Should().Be(EstadoEmail.Pendiente);
        }

        email.RegistrarFallo("500", Now).Should().BeTrue();
        email.Estado.Should().Be(EstadoEmail.Fallido);
        email.Intentos.Should().Be(EmailSaliente.MaxIntentos);
        email.UltimoError.Should().Be("500");
    }

    [Fact]
    public void RegistrarFallo_TruncatesTheError()
    {
        var email = Email();

        email.RegistrarFallo(new string('x', 2000), Now);

        email.UltimoError.Should().HaveLength(EmailSaliente.UltimoErrorMaxLength);
    }

    [Fact]
    public void MarcarEnviado_StoresTheProviderIdAndCannotBeSentTwice()
    {
        var email = Email();
        email.RegistrarFallo("timeout", Now);

        email.MarcarEnviado("re_123", Now.AddMinutes(1));

        (email.Estado, email.ProveedorId, email.EnviadoEn, email.UltimoError, email.Intentos)
            .Should().Be((EstadoEmail.Enviado, "re_123", Now.AddMinutes(1), (string?)null, 2));
        var act = () => email.MarcarEnviado("re_456", Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithUnsubscribeUrl_StoresItAndRejectsAnInvalidOne()
    {
        EmailSaliente.Create("a@b.c", null, "Asunto", "h", "t", "Tipo", null, Now, " https://web.test/api/lista-espera/baja?token=abc ")
            .BajaUrl.Should().Be("https://web.test/api/lista-espera/baja?token=abc");
        Email().BajaUrl.Should().BeNull();

        var relativa = () => EmailSaliente.Create("a@b.c", null, "Asunto", "h", "t", "Tipo", null, Now, "/baja?token=abc");
        var larga = () => EmailSaliente.Create("a@b.c", null, "Asunto", "h", "t", "Tipo", null, Now, "https://web.test/" + new string('a', 500));
        relativa.Should().Throw<DomainException>();
        larga.Should().Throw<DomainException>();
    }
}

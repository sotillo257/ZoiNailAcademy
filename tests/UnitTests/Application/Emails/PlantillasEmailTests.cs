using FluentAssertions;
using ZoiNailAcademy.Application.Emails.Plantillas;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Emails;

public class PlantillasEmailTests
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    private static readonly EmailMarca Marca = new(
        "Zoi Nail Academy", "Calle Mayor 1, Madrid", "https://web.test", Madrid, "zoi@academia.test", "34600111222");

    // Saturday 5 December 2026, 09:00 UTC = 10:00 in Madrid.
    private static readonly DateTimeOffset Inicio = new(2026, 12, 5, 9, 0, 0, TimeSpan.Zero);

    private static ReservaEmailModelo Reserva(string nombre = "Lucía Pérez", MetodoPago metodo = MetodoPago.Stripe) => new(
        Guid.NewGuid(), Guid.Parse("11111111-1111-1111-1111-111111111111"), "CUR-7KQ4XM", "Acrílico", "acrilico", Inicio,
        "Calle Mayor 1", 149m, metodo, nombre, "lucia@mail.com", "+34612345678",
        new DateTimeOffset(2026, 12, 1, 18, 30, 0, TimeSpan.Zero), "pi_123");

    private static readonly DatosPagoEmail Pago = new("Zoi Nail Academy", "ES12 3456", "600111222", "https://wa.me/34600111222?text=Hola");

    public static TheoryData<string, Func<ReservaEmailModelo, EmailContenido>> PlantillasDeReserva => new()
    {
        { "Reserva confirmada · Acrílico · CUR-7KQ4XM", r => ReservaPagadaCompradorEmail.Crear(r, Marca) },
        { "Nueva inscripción · Acrílico 05/12/2026 · CUR-7KQ4XM", r => ReservaPagadaClientaEmail.Crear(r, 3, Marca) },
        { "Completa tu pago · CUR-7KQ4XM", r => ReservaManualCompradorEmail.Crear(r, Pago, Marca) },
        { "Reserva pendiente de pago · CUR-7KQ4XM", r => ReservaManualClientaEmail.Crear(r, Marca) },
        { "Tu reserva ha caducado · CUR-7KQ4XM", r => ReservaExpiradaCompradorEmail.Crear(r, Marca) },
        { "⚠ Pago recibido sin plaza · CUR-7KQ4XM", r => PagoSinPlazaClientaEmail.Crear(r, Marca) },
    };

    [Theory]
    [MemberData(nameof(PlantillasDeReserva))]
    public void Reserva_ContainsCodeSpanishDateAndAmountInBothVersions(
        string asunto, Func<ReservaEmailModelo, EmailContenido> plantilla)
    {
        var email = plantilla(Reserva());

        email.Asunto.Should().Be(asunto);
        foreach (var version in new[] { email.Html, email.Texto })
        {
            version.Should().Contain("CUR-7KQ4XM");
            version.Should().Contain("sábado, 5 de diciembre de 2026, 10:00");
        }

        if (!asunto.StartsWith("Tu reserva ha caducado", StringComparison.Ordinal))
        {
            email.Html.Should().Contain("149,00 €");
            email.Texto.Should().Contain("149,00 €");
        }

        email.Html.Should().Contain("Zoi Nail Academy").And.Contain("https://web.test/privacidad").And.Contain("max-width:600px");
        email.Texto.Should().NotContain("<");
    }

    [Theory]
    [MemberData(nameof(PlantillasDeReserva))]
    public void Reserva_EscapesTheBuyersName(string _, Func<ReservaEmailModelo, EmailContenido> plantilla)
    {
        var email = plantilla(Reserva(nombre: "<script>alert(1)</script>"));

        email.Html.Should().NotContain("<script>");
        if (email.Texto.Contains("<script>alert(1)</script>"))
        {
            email.Html.Should().Contain("&lt;script&gt;");
        }
    }

    [Fact]
    public void ReservaManualComprador_ShowsBankDetailsDeadlineWhatsAppAndTheBookingPage()
    {
        var email = ReservaManualCompradorEmail.Crear(Reserva(metodo: MetodoPago.Manual), Pago, Marca);

        email.Html.Should().Contain("ES12 3456").And.Contain("600111222")
            .And.Contain("martes, 1 de diciembre de 2026, 19:30")
            .And.Contain("https://wa.me/34600111222?text=Hola")
            .And.Contain("https://web.test/reserva/CUR-7KQ4XM");
    }

    [Fact]
    public void ReservaPagadaClienta_LinksTheEditionAndShowsFreeSeats()
    {
        var email = ReservaPagadaClientaEmail.Crear(Reserva(), 3, Marca);

        email.Html.Should().Contain("https://web.test/admin/ediciones/11111111-1111-1111-1111-111111111111");
        email.Texto.Should().Contain("Plazas libres: 3").And.Contain("Teléfono: +34612345678");
    }

    [Fact]
    public void ReservaExpiradaComprador_OffersToBookAgainAndToAnswerIfAlreadyPaid()
    {
        var email = ReservaExpiradaCompradorEmail.Crear(Reserva(metodo: MetodoPago.Manual), Marca);

        email.Html.Should().Contain("https://web.test/reservar/acrilico").And.Contain("Si ya pagaste", Exactly.Once()).And.Contain("WhatsApp");
    }

    [Fact]
    public void ListaEspera_ShowsTheContactAndPreferredDate()
    {
        var email = ListaEsperaClientaEmail.Crear(
            new ListaEsperaEmailModelo("Acrílico", "José <b>", "jose@mail.com", "+34612345678", "Sábados", Inicio), Marca);

        email.Asunto.Should().Be("Nuevo interesado · Acrílico");
        email.Html.Should().Contain("José &lt;b&gt;").And.Contain("Sábados").And.Contain("sábado, 5 de diciembre de 2026, 10:00");
    }

    [Fact]
    public void Contacto_KeepsLineBreaksEscapedAndTheAcknowledgementDoesNotRepeatTheMessage()
    {
        var modelo = new ContactoEmailModelo("Ana", "ana@mail.com", null, "Hola\n<a href=x>spam</a>");

        var clienta = ContactoClientaEmail.Crear(modelo, Marca);
        var acuse = ContactoAcuseEmail.Crear(modelo, Marca);

        clienta.Asunto.Should().Be("Mensaje de la web · Ana");
        clienta.Html.Should().Contain("Hola<br>&lt;a href=x&gt;spam&lt;/a&gt;").And.NotContain("Teléfono");
        acuse.Asunto.Should().Be("Hemos recibido tu mensaje");
        acuse.Html.Should().NotContain("spam");
    }

    [Fact]
    public void Formato_UsesSpanishLongDatesAndCommaDecimals()
    {
        EmailFormato.FechaLarga(Inicio, Madrid).Should().Be("sábado, 5 de diciembre de 2026, 10:00");
        EmailFormato.FechaCorta(Inicio, Madrid).Should().Be("05/12/2026");
        EmailFormato.Importe(1149.5m).Should().Be("1149,50 €".Replace("1149", "1.149"));
    }

    private static AvisoNuevasEdicionesModelo Aviso(string nombre = "Lucía Pérez") => new(
        "Acrílico", "acrilico", nombre, [new(Inicio.AddDays(14), 1), new(Inicio, 6)], "tok_En-123");

    [Fact]
    public void AvisoNuevasEdiciones_ListsTheDatesWithSeatsBookingButtonAndUnsubscribeLink()
    {
        var email = AvisoNuevasEdicionesEmail.Crear(Aviso(), Marca);

        email.Asunto.Should().Be("Nuevas fechas para Acrílico");
        foreach (var version in new[] { email.Html, email.Texto })
        {
            version.Should().Contain("Hola, Lucía Pérez. Hay nuevas fechas para Acrílico:")
                .And.Contain("sábado, 5 de diciembre de 2026, 10:00").And.Contain("Quedan 6 plazas")
                .And.Contain("sábado, 19 de diciembre de 2026, 10:00").And.Contain("Queda 1 plaza")
                .And.Contain("pueden agotarse")
                .And.Contain("https://web.test/reservar/acrilico")
                .And.Contain("te apuntaste a la lista de espera de Acrílico")
                .And.Contain("Darme de baja de los avisos de Acrílico");
        }

        email.Html.Should().Contain("https://web.test/baja?token=tok_En-123&amp;curso=acrilico");
        email.Texto.Should().Contain("https://web.test/baja?token=tok_En-123&curso=acrilico").And.NotContain("<");
        email.Html.IndexOf("5 de diciembre", StringComparison.Ordinal)
            .Should().BeLessThan(email.Html.IndexOf("19 de diciembre", StringComparison.Ordinal), "dates are sorted");
        AvisoNuevasEdicionesEmail.UrlBajaUnClic(Marca, "tok_En-123")
            .Should().Be("https://web.test/api/lista-espera/baja?token=tok_En-123");
    }

    [Fact]
    public void AvisoNuevasEdiciones_EscapesTheNameAndUsesTheSingularForOneDate()
    {
        var email = AvisoNuevasEdicionesEmail.Crear(
            Aviso("<b>Ana</b>") with { Fechas = [new(Inicio, 3)] }, Marca);

        email.Html.Should().NotContain("<b>Ana</b>").And.Contain("&lt;b&gt;Ana&lt;/b&gt;");
        email.Texto.Should().Contain("Hay una nueva fecha para Acrílico:");
    }

    [Theory]
    [InlineData(1, "Aviso enviado a 1 interesado · Acrílico")]
    [InlineData(12, "Aviso enviado a 12 interesados · Acrílico")]
    public void AvisoNuevasEdicionesClienta_SummarizesHowManyPeopleWereEmailed(int interesados, string asunto)
    {
        var email = AvisoNuevasEdicionesClientaEmail.Crear(
            new AvisoNuevasEdicionesClientaModelo("Acrílico", interesados, [new(Inicio, 6)]), Marca);

        email.Asunto.Should().Be(asunto);
        email.Texto.Should().Contain("sábado, 5 de diciembre de 2026, 10:00").And.Contain("6 libres")
            .And.Contain("https://web.test/admin/lista-espera").And.NotContain("baja?token");
    }
}

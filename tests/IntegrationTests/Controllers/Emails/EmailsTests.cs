using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Controllers.Reservas;
using IntegrationTests.TestSupport;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Application.Emails.EnviarEmailsPendientes;
using ZoiNailAcademy.Application.Emails.LimpiarEmailsEnviados;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;
using ZoiNailAcademy.Infrastructure.Email;

namespace IntegrationTests.Controllers.Emails;

/// <summary>Outbox: emails enqueued by the domain events, the sending job with retries, contact form and preview.</summary>
public class EmailsTests(CustomWebApplicationFactory factory) : ReservasTestBase(factory)
{
    private async Task<List<EmailSaliente>> EmailsDeAsync(Guid referenciaId)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().EmailsSalientes.AsNoTracking()
            .Where(e => e.ReferenciaId == referenciaId).ToListAsync();
    }

    private async Task<List<EmailSaliente>> EmailsParaAsync(string para, string tipo)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().EmailsSalientes.AsNoTracking()
            .Where(e => e.Tipo == tipo && (e.Para == para || e.ReplyTo == para)).ToListAsync();
    }

    private async Task PublicarAsync(INotification evento)
    {
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(evento);
    }

    [Fact]
    public async Task ManualBookingConfirmed_EnqueuesTwoEmailsPerEventAndNeverDuplicates()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarManualOkAsync(edicion.Id);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;
        var token = (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

        var response = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{reserva.Id}/confirmar",
            content: JsonContent.Create(new { version = await VersionAsync(creada.Codigo) }), accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        await PublicarAsync(new ReservaPagadaEvent(reserva.Id));
        await PublicarAsync(new ReservaManualCreadaEvent(reserva.Id));

        var emails = await EmailsDeAsync(reserva.Id);
        emails.Select(e => (e.Tipo, e.Para)).Should().BeEquivalentTo(
        [
            (TiposEmail.ReservaManualComprador, reserva.Email),
            (TiposEmail.ReservaManualClienta, CustomWebApplicationFactory.AvisosClienta),
            (TiposEmail.ReservaPagadaComprador, reserva.Email),
            (TiposEmail.ReservaPagadaClienta, CustomWebApplicationFactory.AvisosClienta)
        ]);
        var confirmacion = emails.Single(e => e.Tipo == TiposEmail.ReservaPagadaComprador);
        confirmacion.Asunto.Should().Be($"Reserva confirmada · Curso de test · {creada.Codigo}");
        confirmacion.ReplyTo.Should().Be(CustomWebApplicationFactory.ClientaReplyTo);
        confirmacion.Estado.Should().Be(EstadoEmail.Pendiente);
        emails.Single(e => e.Tipo == TiposEmail.ReservaManualComprador).Html.Should().Contain(CustomWebApplicationFactory.PagoIban);
    }

    [Fact]
    public async Task CardPaymentWebhook_EnqueuesBuyerAndClientEmails()
    {
        var edicion = await CrearEdicionReservableAsync();
        var creada = await ReservarOkAsync(edicion.Id);
        var reserva = (await BuscarReservaAsync(creada.Codigo))!;

        (await EnviarWebhookAsync("checkout.session.completed", reserva.StripeSessionId!, reserva.Id.ToString()))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await EmailsDeAsync(reserva.Id)).Select(e => e.Tipo).Should().BeEquivalentTo(
            [TiposEmail.ReservaPagadaComprador, TiposEmail.ReservaPagadaClienta]);
    }

    [Theory]
    [InlineData(MetodoPago.Stripe, 0)]
    [InlineData(MetodoPago.Manual, 1)]
    public async Task ExpiredBooking_OnlyManualBuyersGetAnEmail(MetodoPago metodo, int esperados)
    {
        var edicion = await CrearEdicionReservableAsync();
        var codigo = metodo == MetodoPago.Stripe
            ? (await ReservarOkAsync(edicion.Id)).Codigo
            : (await ReservarManualOkAsync(edicion.Id)).Codigo;
        await ModificarReservaAsync(codigo, r => r.Expirar(Now));
        var reserva = (await BuscarReservaAsync(codigo))!;

        await PublicarAsync(new ReservaExpiradaEvent(reserva.Id));

        (await EmailsDeAsync(reserva.Id)).Count(e => e.Tipo == TiposEmail.ReservaExpiradaComprador).Should().Be(esperados);
    }

    [Fact]
    public async Task WaitingListSignUp_NotifiesTheClientEachTime()
    {
        var curso = await CrearCursoEnBdAsync();
        var email = $"{Guid.NewGuid():N}@mail.test";
        object Formulario() => new
        {
            cursoId = curso.Id, nombre = "José Núñez", email, telefono = "612345678", aceptaPrivacidad = true, website = ""
        };

        await SendAsync(HttpMethod.Post, "/api/lista-espera", content: JsonContent.Create(Formulario()));
        await SendAsync(HttpMethod.Post, "/api/lista-espera", content: JsonContent.Create(Formulario()));

        using var scope = Factory.Services.CreateScope();
        var avisos = await scope.ServiceProvider.GetRequiredService<AppDbContext>().EmailsSalientes.AsNoTracking()
            .Where(e => e.Tipo == TiposEmail.ListaEsperaClienta && e.Texto.Contains(email)).ToListAsync();
        avisos.Should().HaveCount(2).And.OnlyContain(e => e.Para == CustomWebApplicationFactory.AvisosClienta);
    }

    [Fact]
    public async Task Contacto_Returns202AndEnqueuesTheMessageWithReplyToTheSender()
    {
        var remitente = $"{Guid.NewGuid():N}@mail.test";

        var response = await SendAsync(HttpMethod.Post, "/api/contacto", content: JsonContent.Create(new
        {
            nombre = "Ana <b>", email = remitente, telefono = "", mensaje = "¿Tenéis curso en marzo?", aceptaPrivacidad = true, website = ""
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var email = (await EmailsParaAsync(remitente, TiposEmail.ContactoClienta)).Should().ContainSingle().Subject;
        (email.Para, email.ReplyTo).Should().Be((CustomWebApplicationFactory.AvisosClienta, remitente));
        email.Asunto.Should().Be("Mensaje de la web · Ana <b>");
        email.Html.Should().Contain("Ana &lt;b&gt;").And.Contain("¿Tenéis curso en marzo?");
        (await EmailsParaAsync(remitente, TiposEmail.ContactoAcuse)).Should().BeEmpty("the acknowledgement is off by default");
    }

    [Fact]
    public async Task Contacto_HoneypotOrInvalid_StoresNothing()
    {
        var remitente = $"{Guid.NewGuid():N}@mail.test";
        object Body(string mensaje, string website) => new
        {
            nombre = "Ana", email = remitente, mensaje, aceptaPrivacidad = true, website
        };

        var honeypot = await SendAsync(HttpMethod.Post, "/api/contacto", content: JsonContent.Create(Body("Mensaje suficientemente largo", "http://spam")));
        var corto = await SendAsync(HttpMethod.Post, "/api/contacto", content: JsonContent.Create(Body("Hola", "")));

        honeypot.StatusCode.Should().Be(HttpStatusCode.Accepted);
        corto.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(corto)).GetProperty("errors").TryGetProperty("Mensaje", out _).Should().BeTrue();
        (await EmailsParaAsync(remitente, TiposEmail.ContactoClienta)).Should().BeEmpty();
    }

    // ---- Sending job (own host with a fake provider and its own clock) ----

    private (WebApplicationFactory<Program> Host, FakeTimeProvider Clock, FakeEmailSender Sender) HostConEmail()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sender = new FakeEmailSender();
        var host = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IEmailSender>(sender);
        }));
        return (host, clock, sender);
    }

    private static async Task<Guid> EncolarAsync(WebApplicationFactory<Program> host, string para)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Dated in the past so it is first in the batch whatever other tests left pending.
        var email = EmailSaliente.Create(para, null, "Asunto", "<p>Hola</p>", "Hola", "Test", Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        db.EmailsSalientes.Add(email);
        await db.SaveChangesAsync();
        return email.Id;
    }

    private static async Task<EmailSaliente> EnviarAsync(WebApplicationFactory<Program> host, Guid id)
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EnviarEmailsPendientesCommand());
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().EmailsSalientes.AsNoTracking().SingleAsync(e => e.Id == id);
    }

    [Fact]
    public async Task Job_ProviderFailsTwice_SendsOnTheThirdAttemptAfterTheBackoff()
    {
        var (host, clock, sender) = HostConEmail();
        await using var _ = host;
        var para = $"{Guid.NewGuid():N}@mail.test";
        sender.FallarPara(para, 2);
        var id = await EncolarAsync(host, para);

        var primero = await EnviarAsync(host, id);
        var antesDeTiempo = await EnviarAsync(host, id);
        clock.Advance(TimeSpan.FromMinutes(1));
        var segundo = await EnviarAsync(host, id);
        clock.Advance(TimeSpan.FromMinutes(5));
        var tercero = await EnviarAsync(host, id);

        (primero.Estado, primero.Intentos).Should().Be((EstadoEmail.Pendiente, 1));
        primero.UltimoError.Should().Contain("503");
        antesDeTiempo.Intentos.Should().Be(1, "the retry is not due yet");
        (segundo.Estado, segundo.Intentos).Should().Be((EstadoEmail.Pendiente, 2));
        (tercero.Estado, tercero.Intentos, tercero.ProveedorId).Should().Be((EstadoEmail.Enviado, 3, $"fake_{id:N}"));
        sender.Enviados.Should().ContainSingle(m => m.Id == id);
    }

    [Fact]
    public async Task Job_ProviderAlwaysFails_MarksTheEmailFailedAfterFiveAttempts()
    {
        var (host, clock, sender) = HostConEmail();
        await using var _ = host;
        var para = $"{Guid.NewGuid():N}@mail.test";
        sender.FallarPara(para, int.MaxValue);
        var id = await EncolarAsync(host, para);

        EmailSaliente email = null!;
        for (var i = 0; i < EmailSaliente.MaxIntentos; i++)
        {
            email = await EnviarAsync(host, id);
            clock.Advance(TimeSpan.FromHours(5));
        }

        var despues = await EnviarAsync(host, id);

        (email.Estado, email.Intentos).Should().Be((EstadoEmail.Fallido, EmailSaliente.MaxIntentos));
        despues.Intentos.Should().Be(EmailSaliente.MaxIntentos, "a failed email is not retried");
    }

    [Fact]
    public async Task Cleanup_DeletesOnlySentEmailsOlderThan90Days()
    {
        var (host, clock, _) = HostConEmail();
        await using var __ = host;
        var viejo = await EncolarAsync(host, $"{Guid.NewGuid():N}@mail.test");
        await EnviarAsync(host, viejo);
        clock.Advance(TimeSpan.FromDays(91));
        var reciente = await EncolarAsync(host, $"{Guid.NewGuid():N}@mail.test");
        await EnviarAsync(host, reciente);

        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new LimpiarEmailsEnviadosCommand());
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.EmailsSalientes.AnyAsync(e => e.Id == viejo)).Should().BeFalse();
        (await db.EmailsSalientes.AnyAsync(e => e.Id == reciente)).Should().BeTrue();
    }

    // ---- Provider selection and preview ----

    [Theory]
    [InlineData("Resend", "", "", "Log", true)]
    [InlineData("Resend", "re_123", "", "Log", true)]
    [InlineData("Resend", "re_123", "Zoi <reservas@zoi.test>", "Resend", false)]
    [InlineData("Log", "", "", "Log", false)]
    [InlineData("Otro", "re_123", "Zoi <reservas@zoi.test>", "Log", true)]
    public void ProviderSelection_WithoutResendConfigured_FallsBackToTheLogWithAWarning(
        string provider, string apiKey, string from, string esperado, bool conAviso)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = provider, ["Resend:ApiKey"] = apiKey, ["Email:From"] = from
        }).Build();

        var seleccion = EmailProveedorSeleccion.Desde(configuration);

        seleccion.Proveedor.Should().Be(esperado);
        (seleccion.Aviso is not null).Should().Be(conAviso);
    }

    [Fact]
    public void SharedHost_WithoutResendKey_UsesTheLogSender()
    {
        Factory.Services.GetRequiredService<IEmailSender>().Should().BeOfType<LogEmailSender>();
    }

    [Fact]
    public async Task Preview_SuperAdminGetsHtmlAdminIsForbiddenAndUnknownTypeIs404()
    {
        var superAdmin = (await LoginAsNewUserAsync(UserRole.SuperAdmin)).AccessToken;
        var admin = (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

        var html = await SendAsync(HttpMethod.Get, $"/api/admin/emails/preview/{TiposEmail.ReservaManualComprador}", accessToken: superAdmin);
        var texto = await SendAsync(HttpMethod.Get, $"/api/admin/emails/preview/{TiposEmail.ReservaManualComprador}?formato=texto", accessToken: superAdmin);
        var prohibido = await SendAsync(HttpMethod.Get, $"/api/admin/emails/preview/{TiposEmail.ReservaManualComprador}", accessToken: admin);
        var anonimo = await SendAsync(HttpMethod.Get, $"/api/admin/emails/preview/{TiposEmail.ReservaManualComprador}");
        var desconocido = await SendAsync(HttpMethod.Get, "/api/admin/emails/preview/NoExiste", accessToken: superAdmin);

        html.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await html.Content.ReadAsStringAsync()).Should().StartWith("<!DOCTYPE html>").And.Contain(CustomWebApplicationFactory.PagoIban);
        texto.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        prohibido.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        anonimo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ShouldBeProblemWithCodeAsync(desconocido, 404, "no_encontrado");
    }
}

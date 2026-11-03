using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Controllers.Catalogo;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Application.AvisosNuevasEdiciones.EnviarAvisos;
using ZoiNailAcademy.Application.Emails;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.ListaEspera;

/// <summary>F11: publishing dates notifies the waiting list (grouped by the job), and the unsubscribe endpoint.</summary>
public class AvisosNuevasEdicionesTests(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    private async Task<string> AdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

    private async Task<EdicionAdminTestDto> PublicarAsync(Guid cursoId, int dias, string token, string estado = "Programada")
    {
        var response = await SendAsync(HttpMethod.Post, "/api/admin/ediciones",
            content: JsonContent.Create(new { cursoId, inicio = Now.AddDays(dias), plazas = 8, estado }), accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await ReadAsync<EdicionAdminTestDto>(response);
    }

    private async Task<EdicionAdminTestDto> CambiarEstadoAsync(EdicionAdminTestDto edicion, string estado, string token)
    {
        var response = await SendAsync(HttpMethod.Put, $"/api/admin/ediciones/{edicion.Id}", content: JsonContent.Create(new
        {
            inicio = edicion.Inicio, plazas = edicion.Plazas, estado, notas = edicion.Notas, version = edicion.Version,
            confirmarCambioFecha = false
        }), accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ReadAsync<EdicionEditadaTestDto>(response)).Edicion;
    }

    /// <returns>The waiting list entry, read back from the database.</returns>
    private async Task<ZoiNailAcademy.Domain.Entities.ListaEspera> ApuntarAsync(Guid cursoId, string nombre = "Lucía Pérez")
    {
        var email = $"{Guid.NewGuid():N}@mail.test";
        var response = await SendAsync(HttpMethod.Post, "/api/lista-espera", content: JsonContent.Create(new
        {
            cursoId, nombre, email, telefono = "612 34 56 78", aceptaPrivacidad = true, website = ""
        }));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await EntradaAsync(email);
    }

    private async Task<ZoiNailAcademy.Domain.Entities.ListaEspera> EntradaAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ListaEspera.AsNoTracking().SingleAsync(e => e.Email == email);
    }

    private async Task<int> EjecutarJobAsync()
    {
        using var scope = Factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EnviarAvisosNuevasEdicionesCommand())).Value;
    }

    private async Task<List<EmailSaliente>> AvisosParaAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().EmailsSalientes.AsNoTracking()
            .Where(e => e.Tipo == TiposEmail.AvisoNuevasEdiciones && e.Para == email).ToListAsync();
    }

    private async Task<bool> CursoPendienteAsync(Guid cursoId)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().AvisosPendientes.AnyAsync(p => p.CursoId == cursoId);
    }

    private Task<HttpResponseMessage> BajaJsonAsync(string token) =>
        SendAsync(HttpMethod.Post, "/api/lista-espera/baja", content: JsonContent.Create(new { token }));

    private string FechaLarga(DateTimeOffset inicio) =>
        ZoiNailAcademy.Application.Emails.Plantillas.EmailFormato.FechaLarga(inicio, TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"));

    [Fact]
    public async Task PublishingADate_AfterTheJob_EmailsEverySubscribedPersonOfThatCourseOnly()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var otroCurso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id, "Lucía");
        var marta = await ApuntarAsync(curso.Id, "Marta");
        var deBaja = await ApuntarAsync(curso.Id, "Baja");
        var deOtroCurso = await ApuntarAsync(otroCurso.Id, "Otra");
        (await BajaJsonAsync(deBaja.BajaToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var edicion = await PublicarAsync(curso.Id, 20, token);

        (await CursoPendienteAsync(curso.Id)).Should().BeTrue();
        (await AvisosParaAsync(lucia.Email)).Should().BeEmpty("the handler only marks the course; the job sends");

        (await EjecutarJobAsync()).Should().BeGreaterThanOrEqualTo(2);

        foreach (var persona in new[] { lucia, marta })
        {
            var aviso = (await AvisosParaAsync(persona.Email)).Should().ContainSingle().Subject;
            aviso.Asunto.Should().Be("Nuevas fechas para Curso de test");
            aviso.Html.Should().Contain(FechaLarga(edicion.Inicio)).And.Contain("Quedan 8 plazas")
                .And.Contain($"https://web.test/reservar/{curso.Slug}");
            aviso.Estado.Should().Be(EstadoEmail.Pendiente);
        }

        (await AvisosParaAsync(deBaja.Email)).Should().BeEmpty();
        (await AvisosParaAsync(deOtroCurso.Email)).Should().BeEmpty();
        (await CursoPendienteAsync(curso.Id)).Should().BeFalse();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.AvisosEnviados.CountAsync(a => a.EdicionId == edicion.Id)).Should().Be(2);
        (await db.EmailsSalientes.AnyAsync(e => e.Tipo == TiposEmail.AvisoNuevasEdicionesClienta
            && e.Para == CustomWebApplicationFactory.AvisosClienta
            && e.Asunto == "Aviso enviado a 2 interesados · Curso de test"
            && e.Texto.Contains(FechaLarga(edicion.Inicio)))).Should().BeTrue();
    }

    [Fact]
    public async Task ThreeDatesPublishedBeforeTheJob_SendOneEmailPerPersonWithTheThree()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);

        var ediciones = new[] { await PublicarAsync(curso.Id, 20, token), await PublicarAsync(curso.Id, 27, token), await PublicarAsync(curso.Id, 34, token) };
        await EjecutarJobAsync();

        var aviso = (await AvisosParaAsync(lucia.Email)).Should().ContainSingle().Subject;
        foreach (var edicion in ediciones)
        {
            aviso.Texto.Should().Contain(FechaLarga(edicion.Inicio));
        }
    }

    [Fact]
    public async Task HidingAndPublishingAgain_DoesNotSendTheSameDateTwice()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);
        var edicion = await PublicarAsync(curso.Id, 20, token);
        await EjecutarJobAsync();

        var oculta = await CambiarEstadoAsync(edicion, "Oculta", token);
        await CambiarEstadoAsync(oculta, "Programada", token);
        (await CursoPendienteAsync(curso.Id)).Should().BeTrue("publishing again marks the course");
        await EjecutarJobAsync();

        (await AvisosParaAsync(lucia.Email)).Should().ContainSingle();
        (await CursoPendienteAsync(curso.Id)).Should().BeFalse();

        // Someone who joins later is told about that same date the next time a date is published.
        var nueva = await ApuntarAsync(curso.Id, "Nueva");
        var otra = await PublicarAsync(curso.Id, 40, token);
        await EjecutarJobAsync();
        (await AvisosParaAsync(lucia.Email)).Should().HaveCount(2);
        var avisoNueva = (await AvisosParaAsync(nueva.Email)).Should().ContainSingle().Subject;
        avisoNueva.Texto.Should().Contain(FechaLarga(edicion.Inicio)).And.Contain(FechaLarga(otra.Inicio));
    }

    [Fact]
    public async Task DateCancelledBeforeTheJob_IsNotNotified()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);
        var edicion = await PublicarAsync(curso.Id, 20, token);

        await CambiarEstadoAsync(edicion, "Cancelada", token);
        await EjecutarJobAsync();

        (await AvisosParaAsync(lucia.Email)).Should().BeEmpty();
        (await CursoPendienteAsync(curso.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task HiddenDate_IsNotNotified()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);

        await PublicarAsync(curso.Id, 20, token, estado: "Oculta");
        await EjecutarJobAsync();

        (await CursoPendienteAsync(curso.Id)).Should().BeFalse();
        (await AvisosParaAsync(lucia.Email)).Should().BeEmpty();
    }

    [Fact]
    public async Task Email_CarriesTheUnsubscribeLinkAndTheOneClickUrlWorksWithGmailsFormBody()
    {
        var token = await AdminTokenAsync();
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);
        await PublicarAsync(curso.Id, 20, token);
        await EjecutarJobAsync();

        var aviso = (await AvisosParaAsync(lucia.Email)).Should().ContainSingle().Subject;
        aviso.BajaUrl.Should().Be($"https://web.test/api/lista-espera/baja?token={lucia.BajaToken}");
        aviso.Html.Should().Contain($"https://web.test/baja?token={lucia.BajaToken}&amp;curso={curso.Slug}");
        aviso.Texto.Should().Contain($"https://web.test/baja?token={lucia.BajaToken}&curso={curso.Slug}");

        // What Gmail does with List-Unsubscribe-Post: POST the header URL with a form body.
        var response = await SendAsync(HttpMethod.Post, new Uri(aviso.BajaUrl!).PathAndQuery,
            content: new FormUrlEncodedContent([new("List-Unsubscribe", "One-Click")]));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntradaAsync(lucia.Email)).BajaEn.Should().BeCloseTo(Now, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Baja_ValidAndInvalidToken_Return204AndOnlyTheValidOneChangesItsEntry()
    {
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id, "Lucía");
        var marta = await ApuntarAsync(curso.Id, "Marta");

        var invalido = await BajaJsonAsync("token-que-no-existe");
        var valido = await BajaJsonAsync(lucia.BajaToken);
        var repetido = await BajaJsonAsync(lucia.BajaToken);
        var vacio = await SendAsync(HttpMethod.Post, "/api/lista-espera/baja");
        var malformado = await SendAsync(HttpMethod.Post, "/api/lista-espera/baja",
            content: new StringContent("{no es json", System.Text.Encoding.UTF8, "application/json"));

        foreach (var response in new[] { invalido, valido, repetido, vacio, malformado })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await EntradaAsync(lucia.Email)).BajaEn.Should().BeCloseTo(Now, TimeSpan.FromMilliseconds(1));
        (await EntradaAsync(marta.Email)).BajaEn.Should().BeNull();

        var token = await AdminTokenAsync();
        var listado = await SendAsync(HttpMethod.Get, $"/api/admin/lista-espera?cursoId={curso.Id}", accessToken: token);
        var items = (await ReadJsonAsync(listado)).GetProperty("items").EnumerateArray()
            .ToDictionary(i => i.GetProperty("email").GetString()!, i => i.GetProperty("baja").GetBoolean());
        items.Should().Equal(new Dictionary<string, bool> { [lucia.Email] = true, [marta.Email] = false });
    }

    [Fact]
    public async Task Baja_TokenInAFormField_Unsubscribes()
    {
        var curso = await CrearCursoEnBdAsync();
        var lucia = await ApuntarAsync(curso.Id);

        var response = await SendAsync(HttpMethod.Post, "/api/lista-espera/baja",
            content: new FormUrlEncodedContent([new("token", lucia.BajaToken)]));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntradaAsync(lucia.Email)).BajaEn.Should().NotBeNull();
    }
}

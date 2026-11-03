using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using IntegrationTests.Controllers.Catalogo;
using IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Application.ListasEspera;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests.Controllers.ListaEspera;

/// <summary>POST /api/lista-espera and the backoffice listing, "contacted" mark and CSV.</summary>
public class ListaEsperaTests(CustomWebApplicationFactory factory) : CatalogoTestBase(factory)
{
    private static object Formulario(
        Guid cursoId, string? email = null, string nombre = "Lucía Pérez", Guid? edicionPreferidaId = null,
        string? fechaPreferida = "Sábados de marzo", string telefono = "612 34 56 78", string website = "",
        bool aceptaPrivacidad = true) => new
    {
        cursoId,
        edicionPreferidaId,
        fechaPreferida,
        nombre,
        email = email ?? $"{Guid.NewGuid():N}@mail.test",
        telefono,
        aceptaPrivacidad,
        textoConsentimiento = "Texto que manda el front y se ignora",
        website
    };

    private Task<HttpResponseMessage> AltaAsync(object formulario) =>
        SendAsync(HttpMethod.Post, "/api/lista-espera", content: JsonContent.Create(formulario));

    private async Task AltaOkAsync(object formulario)
    {
        var response = await AltaAsync(formulario);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private async Task<List<ZoiNailAcademy.Domain.Entities.ListaEspera>> EntradasAsync(Guid cursoId)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ListaEspera.AsNoTracking()
            .Where(e => e.CursoId == cursoId).ToListAsync();
    }

    private async Task<string> AdminTokenAsync() => (await LoginAsNewUserAsync(UserRole.Admin)).AccessToken;

    private async Task<ListaEsperaPaginaTestDto> ListarAsync(string token, string query)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/admin/lista-espera?{query}", accessToken: token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadAsync<ListaEsperaPaginaTestDto>(response);
    }

    [Fact]
    public async Task Alta_NewPerson_Returns201WithoutBodyStoresTheBackendConsentAndPublishesTheEvent()
    {
        var curso = await CrearCursoEnBdAsync();

        var response = await AltaAsync(Formulario(curso.Id, email: " Lucia@Mail.TEST "));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var entrada = (await EntradasAsync(curso.Id)).Should().ContainSingle().Subject;
        entrada.Email.Should().Be("lucia@mail.test");
        entrada.Telefono.Should().Be("+34612345678");
        entrada.FechaPreferida.Should().Be("Sábados de marzo");
        entrada.TextoConsentimiento.Should().Be(ConsentimientoListaEspera.V1);
        entrada.BajaToken.Should().HaveLength(43);
        EventRecorder.Count<ListaEsperaAltaEvent>(e => e.ListaEsperaId == entrada.Id).Should().Be(1);
    }

    [Fact]
    public async Task Alta_SamePersonAndCourse_UpdatesWithoutDuplicatingAndResetsContactado()
    {
        var curso = await CrearCursoEnBdAsync();
        var email = $"{Guid.NewGuid():N}@mail.test";
        await AltaOkAsync(Formulario(curso.Id, email: email, nombre: "Primera"));
        var primera = (await EntradasAsync(curso.Id)).Single();
        var token = await AdminTokenAsync();
        await SendAsync(HttpMethod.Put, $"/api/admin/lista-espera/{primera.Id}/contactado",
            content: JsonContent.Create(new { contactado = true }), accessToken: token);

        await AltaOkAsync(Formulario(curso.Id, email: email.ToUpperInvariant(), nombre: "Segunda", fechaPreferida: null));

        var entrada = (await EntradasAsync(curso.Id)).Should().ContainSingle().Subject;
        entrada.Id.Should().Be(primera.Id);
        entrada.Nombre.Should().Be("Segunda");
        entrada.FechaPreferida.Should().BeNull();
        entrada.Contactado.Should().BeFalse();
        entrada.ContactadoEn.Should().BeNull();
        EventRecorder.Count<ListaEsperaAltaEvent>(e => e.ListaEsperaId == entrada.Id).Should().Be(2);
    }

    [Fact]
    public async Task Alta_SameEmailOnAnotherCourse_CreatesANewEntry()
    {
        var curso = await CrearCursoEnBdAsync();
        var otro = await CrearCursoEnBdAsync();
        var email = $"{Guid.NewGuid():N}@mail.test";

        await AltaOkAsync(Formulario(curso.Id, email: email));
        await AltaOkAsync(Formulario(otro.Id, email: email));

        (await EntradasAsync(curso.Id)).Should().ContainSingle();
        (await EntradasAsync(otro.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task Alta_SimultaneousSignUpsOfTheSamePerson_KeepOneEntry()
    {
        var curso = await CrearCursoEnBdAsync();
        var email = $"{Guid.NewGuid():N}@mail.test";

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => AltaAsync(Formulario(curso.Id, email: email))));

        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Created);
        (await EntradasAsync(curso.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task Alta_InactiveOrUnknownCourse_Returns404()
    {
        var inactivo = await CrearCursoEnBdAsync(activo: false);

        await ShouldBeProblemWithCodeAsync(await AltaAsync(Formulario(inactivo.Id)), 404, "no_encontrado");
        await ShouldBeProblemWithCodeAsync(await AltaAsync(Formulario(Guid.NewGuid())), 404, "no_encontrado");
        (await EntradasAsync(inactivo.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Alta_PreferredEditionOfAnotherCourse_Returns400()
    {
        var curso = await CrearCursoEnBdAsync();
        var otro = await CrearCursoEnBdAsync();
        var edicionAjena = await CrearEdicionEnBdAsync(otro, Now.AddDays(20));
        var edicionPropia = await CrearEdicionEnBdAsync(curso, Now.AddDays(20));

        var response = await AltaAsync(Formulario(curso.Id, edicionPreferidaId: edicionAjena.Id));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("EdicionPreferidaId", out _).Should().BeTrue();
        await AltaOkAsync(Formulario(curso.Id, edicionPreferidaId: edicionPropia.Id));
        (await EntradasAsync(curso.Id)).Should().ContainSingle().Which.EdicionPreferidaId.Should().Be(edicionPropia.Id);
    }

    [Fact]
    public async Task Alta_Honeypot_Returns201AndStoresNothing()
    {
        var curso = await CrearCursoEnBdAsync();

        var response = await AltaAsync(Formulario(curso.Id, website: "http://spam"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EntradasAsync(curso.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Alta_InvalidFields_Returns400PerField()
    {
        var curso = await CrearCursoEnBdAsync();

        var response = await AltaAsync(Formulario(curso.Id, email: "no-es-email", telefono: "123", aceptaPrivacidad: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        errors.TryGetProperty("Email", out _).Should().BeTrue();
        errors.TryGetProperty("Telefono", out _).Should().BeTrue();
        errors.TryGetProperty("AceptaPrivacidad", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Listar_FiltersSearchesWithoutAccentsAndOrdersNotContactedFirst()
    {
        var curso = await CrearCursoEnBdAsync();
        var otro = await CrearCursoEnBdAsync();
        await AltaOkAsync(Formulario(curso.Id, nombre: "José Núñez"));
        Factory.Clock.Advance(TimeSpan.FromSeconds(1));
        await AltaOkAsync(Formulario(curso.Id, nombre: "Ana Contactada", fechaPreferida: null));
        Factory.Clock.Advance(TimeSpan.FromSeconds(1));
        await AltaOkAsync(Formulario(curso.Id, nombre: "Marta Última"));
        await AltaOkAsync(Formulario(otro.Id, nombre: "José De Otro Curso"));
        var token = await AdminTokenAsync();
        var ana = (await EntradasAsync(curso.Id)).Single(e => e.Nombre == "Ana Contactada");
        await SendAsync(HttpMethod.Put, $"/api/admin/lista-espera/{ana.Id}/contactado",
            content: JsonContent.Create(new { contactado = true }), accessToken: token);

        var todos = await ListarAsync(token, $"cursoId={curso.Id}");
        var jose = await ListarAsync(token, $"cursoId={curso.Id}&q=Jose");
        var joseTodos = await ListarAsync(token, "q=JOSÉ%20N");
        var contactados = await ListarAsync(token, $"cursoId={curso.Id}&contactado=true");
        var pagina2 = await ListarAsync(token, $"cursoId={curso.Id}&page=2&pageSize=2");

        todos.Items.Select(i => i.Nombre).Should().Equal("José Núñez", "Marta Última", "Ana Contactada");
        todos.Total.Should().Be(3);
        jose.Items.Should().ContainSingle().Which.Nombre.Should().Be("José Núñez");
        joseTodos.Items.Select(i => i.Nombre).Should().Contain("José Núñez").And.NotContain("José De Otro Curso");
        contactados.Items.Should().ContainSingle().Which.Should().Match<ListaEsperaAdminTestDto>(
            i => i.Nombre == "Ana Contactada" && i.Contactado && i.ContactadoEn != null);
        (pagina2.Total, pagina2.Page, pagina2.PageSize).Should().Be((3, 2, 2));
        pagina2.Items.Should().ContainSingle().Which.Nombre.Should().Be("Ana Contactada");
        var item = todos.Items[0];
        (item.CursoId, item.Curso, item.FechaPreferida, item.Telefono, item.Baja).Should()
            .Be((curso.Id, "Curso de test", "Sábados de marzo", "+34612345678", false));
    }

    [Fact]
    public async Task Listar_PreferredEdition_IsFormattedInMadridTime()
    {
        var curso = await CrearCursoEnBdAsync();
        var inicio = new DateTimeOffset(Now.Year + 1, 7, 4, 8, 0, 0, TimeSpan.Zero); // 10:00 in Madrid (CEST)
        var edicion = await CrearEdicionEnBdAsync(curso, inicio);
        await AltaOkAsync(Formulario(curso.Id, edicionPreferidaId: edicion.Id));

        var pagina = await ListarAsync(await AdminTokenAsync(), $"cursoId={curso.Id}");

        pagina.Items.Should().ContainSingle().Which.EdicionPreferida.Should().Be($"04/07/{Now.Year + 1} 10:00");
    }

    [Fact]
    public async Task Listar_PageSizeOverTheMaximum_Returns400()
    {
        var response = await SendAsync(
            HttpMethod.Get, "/api/admin/lista-espera?pageSize=201", accessToken: await AdminTokenAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MarcarContactado_MarksAndUnmarksAndKeepsNotesUnlessSent()
    {
        var curso = await CrearCursoEnBdAsync();
        await AltaOkAsync(Formulario(curso.Id));
        var id = (await EntradasAsync(curso.Id)).Single().Id;
        var token = await AdminTokenAsync();
        Task<HttpResponseMessage> Put(object body) =>
            SendAsync(HttpMethod.Put, $"/api/admin/lista-espera/{id}/contactado", content: JsonContent.Create(body), accessToken: token);

        var marcada = await ReadAsync<ListaEsperaAdminTestDto>(await Put(new { contactado = true, notas = "Llamada, vuelve en marzo" }));
        var sinNotas = await ReadAsync<ListaEsperaAdminTestDto>(await Put(new { contactado = true }));
        var desmarcada = await ReadAsync<ListaEsperaAdminTestDto>(await Put(new { contactado = false, notas = (string?)null }));

        marcada.Contactado.Should().BeTrue();
        marcada.ContactadoEn.Should().NotBeNull();
        marcada.Notas.Should().Be("Llamada, vuelve en marzo");
        sinNotas.Notas.Should().Be("Llamada, vuelve en marzo");
        sinNotas.ContactadoEn.Should().BeCloseTo(marcada.ContactadoEn!.Value, TimeSpan.FromMilliseconds(1));
        desmarcada.Contactado.Should().BeFalse();
        desmarcada.ContactadoEn.Should().BeNull();
        desmarcada.Notas.Should().BeNull();
    }

    [Fact]
    public async Task MarcarContactado_UnknownEntryOrTooLongNotes_Returns404Or400()
    {
        var curso = await CrearCursoEnBdAsync();
        await AltaOkAsync(Formulario(curso.Id));
        var id = (await EntradasAsync(curso.Id)).Single().Id;
        var token = await AdminTokenAsync();

        var desconocida = await SendAsync(HttpMethod.Put, $"/api/admin/lista-espera/{Guid.NewGuid()}/contactado",
            content: JsonContent.Create(new { contactado = true }), accessToken: token);
        var larga = await SendAsync(HttpMethod.Put, $"/api/admin/lista-espera/{id}/contactado",
            content: JsonContent.Create(new { contactado = true, notas = new string('a', 501) }), accessToken: token);

        await ShouldBeProblemWithCodeAsync(desconocida, 404, "no_encontrado");
        larga.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Csv_HasBomSemicolonsMadridDatesAndFormulaProtection()
    {
        var curso = await CrearCursoEnBdAsync();
        await AltaOkAsync(Formulario(curso.Id, nombre: "José; \"Pepe\"", fechaPreferida: "=Sábados"));

        var response = await SendAsync(
            HttpMethod.Get, $"/api/admin/lista-espera.csv?cursoId={curso.Id}", accessToken: await AdminTokenAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.ToString().Should().Be("text/csv; charset=utf-8");
        var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var hoy = TimeZoneInfo.ConvertTime(Now, madrid);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be($"lista-espera-{curso.Slug}-{hoy:yyyyMMdd}.csv");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var lineas = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n");
        lineas[0].Should().Be("Curso;Nombre;Teléfono;Email;Fecha preferida;Edición preferida;Contactado;Fecha de alta;Notas;Baja");
        var entrada = (await EntradasAsync(curso.Id)).Single();
        var alta = TimeZoneInfo.ConvertTime(entrada.CreatedAt, madrid);
        lineas[1].Should().Be(
            $"Curso de test;\"José; \"\"Pepe\"\"\";'+34612345678;{entrada.Email};'=Sábados;;No;{alta:dd/MM/yyyy HH:mm};;No");
        lineas.Should().HaveCount(3); // header, one row and the final CRLF
    }

    [Fact]
    public async Task Csv_UnknownCourse_Returns404()
    {
        var response = await SendAsync(
            HttpMethod.Get, $"/api/admin/lista-espera.csv?cursoId={Guid.NewGuid()}", accessToken: await AdminTokenAsync());

        await ShouldBeProblemWithCodeAsync(response, 404, "no_encontrado");
    }

    [Theory]
    [InlineData("GET", "/api/admin/lista-espera")]
    [InlineData("GET", "/api/admin/lista-espera.csv")]
    [InlineData("PUT", "/api/admin/lista-espera/00000000-0000-0000-0000-000000000001/contactado")]
    public async Task AdminEndpoints_WithoutToken_Return401(string method, string url)
    {
        var response = await SendAsync(new HttpMethod(method), url, content: JsonContent.Create(new { contactado = true }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public record ListaEsperaAdminTestDto(
    Guid Id, Guid CursoId, string Curso, string? EdicionPreferida, string? FechaPreferida, string Nombre, string Email,
    string Telefono, bool Contactado, DateTimeOffset? ContactadoEn, bool Baja, string? Notas, DateTimeOffset CreadoEn);

public record ListaEsperaPaginaTestDto(List<ListaEsperaAdminTestDto> Items, int Total, int Page, int PageSize);

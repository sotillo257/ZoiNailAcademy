using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests.Controllers;

public class NotesControllerTests : IntegrationTestBase
{
    public NotesControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    private async Task<string> RegisterUserAsync()
    {
        var payload = new { email = UniqueEmail(), password = "password123", displayName = "Usuario" };
        var response = await PostAsJsonFromIpAsync("/auth/register", payload, NextTestClientIp());
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions))!;
        return auth.AccessToken;
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string accessToken, object? payload = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string accessToken, object? payload = null)
    {
        using var request = AuthenticatedRequest(method, url, accessToken, payload);
        return await Client.SendAsync(request);
    }

    private async Task<NoteResponseDto> CreateNoteAsync(string accessToken, string title, string content = "")
    {
        var response = await SendAsync(HttpMethod.Post, "/notes", accessToken, new { title, content });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<NoteResponseDto>(JsonOptions))!;
    }

    [Fact]
    public async Task Post_ValidNote_Returns201WithLocationAndBody()
    {
        var accessToken = await RegisterUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/notes", accessToken, new { title = "  Ideas  ", content = "Contenido" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<NoteResponseDto>(JsonOptions);
        body!.Title.Should().Be("Ideas");
        body.Content.Should().Be("Contenido");
        body.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        body.UpdatedAt.Should().BeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/notes/{body.Id}");
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400ValidationProblem()
    {
        var accessToken = await RegisterUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/notes", accessToken, new { title = "", content = "Contenido" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>(JsonOptions);
        problem!.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/notes", new { title = "Ideas", content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwnNote_Returns200()
    {
        var accessToken = await RegisterUserAsync();
        var created = await CreateNoteAsync(accessToken, "Ideas", "Contenido");

        var response = await SendAsync(HttpMethod.Get, $"/notes/{created.Id}", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NoteResponseDto>(JsonOptions);
        body!.Id.Should().Be(created.Id);
        body.Content.Should().Be("Contenido");
    }

    [Fact]
    public async Task GetById_OtherUsersNote_Returns404ProblemJson()
    {
        var ownerToken = await RegisterUserAsync();
        var otherToken = await RegisterUserAsync();
        var created = await CreateNoteAsync(ownerToken, "Privada");

        var response = await SendAsync(HttpMethod.Get, $"/notes/{created.Id}", otherToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetList_ReturnsOnlyOwnNotesNewestFirstAndPaged()
    {
        var accessToken = await RegisterUserAsync();
        var otherToken = await RegisterUserAsync();
        await CreateNoteAsync(accessToken, "Primera");
        await CreateNoteAsync(accessToken, "Segunda");
        await CreateNoteAsync(accessToken, "Tercera");
        await CreateNoteAsync(otherToken, "Ajena");

        var response = await SendAsync(HttpMethod.Get, "/notes?page=1&pageSize=2", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedNotesResponseDto>(JsonOptions);
        body!.TotalCount.Should().Be(3);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
        body.Items.Select(n => n.Title).Should().Equal("Tercera", "Segunda");
    }

    [Fact]
    public async Task GetList_InvalidPageSize_Returns400()
    {
        var accessToken = await RegisterUserAsync();

        var response = await SendAsync(HttpMethod.Get, "/notes?pageSize=0", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_OwnNote_Returns200AndSetsUpdatedAt()
    {
        var accessToken = await RegisterUserAsync();
        var created = await CreateNoteAsync(accessToken, "Original", "Texto");

        var response = await SendAsync(HttpMethod.Put, $"/notes/{created.Id}", accessToken, new { title = "Editada", content = "Nuevo texto" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NoteResponseDto>(JsonOptions);
        body!.Id.Should().Be(created.Id);
        body.Title.Should().Be("Editada");
        body.Content.Should().Be("Nuevo texto");
        body.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Put_OtherUsersNote_Returns404()
    {
        var ownerToken = await RegisterUserAsync();
        var otherToken = await RegisterUserAsync();
        var created = await CreateNoteAsync(ownerToken, "Privada");

        var response = await SendAsync(HttpMethod.Put, $"/notes/{created.Id}", otherToken, new { title = "Hackeada", content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OwnNote_Returns204AndNoteIsGone()
    {
        var accessToken = await RegisterUserAsync();
        var created = await CreateNoteAsync(accessToken, "Temporal");

        var deleteResponse = await SendAsync(HttpMethod.Delete, $"/notes/{created.Id}", accessToken);
        var getResponse = await SendAsync(HttpMethod.Get, $"/notes/{created.Id}", accessToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

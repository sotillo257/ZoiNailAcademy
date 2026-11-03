using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests;

[Collection("IntegrationTests")]
public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static int _ipCounter;

    protected readonly CustomWebApplicationFactory Factory;

    /// <summary>
    /// Client without a cookie container: the rt cookie is Secure and the test server is http, so
    /// tests read Set-Cookie and send the Cookie header explicitly.
    /// </summary>
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    /// <summary>
    /// Each call returns a fresh fake client IP so tests hitting rate-limited endpoints don't share
    /// the rate limiter's partition with unrelated tests running against the shared factory.
    /// </summary>
    protected static string NextTestClientIp()
    {
        var n = Interlocked.Increment(ref _ipCounter);
        return $"10.{n / 65536 % 256}.{n / 256 % 256}.{n % 256}";
    }

    protected Task<HttpResponseMessage> PostAsJsonFromIpAsync<T>(string url, T payload, string clientIp) =>
        SendAsync(HttpMethod.Post, url, clientIp, JsonContent.Create(payload));

    protected Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string? clientIp = null, HttpContent? content = null,
        string? refreshCookie = null, string? accessToken = null, HttpClient? client = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add("X-Forwarded-For", clientIp ?? NextTestClientIp());
        if (refreshCookie is not null)
        {
            request.Headers.Add("Cookie", $"rt={refreshCookie}");
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return (client ?? Client).SendAsync(request);
    }

    protected static string UniqueEmail() => $"user-{Guid.NewGuid():N}@mail.com";

    /// <summary>Creates a user directly in the database (registration does not exist).</summary>
    protected async Task<string> CreateUserAsync(UserRole role, string password = "password-123")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = UniqueEmail();
        db.Users.Add(User.Create(email, "Usuario de test", hasher.Hash(password), role));
        await db.SaveChangesAsync();
        return email;
    }

    protected async Task<LoginResult> LoginAsync(string email, string password = "password-123", HttpClient? client = null)
    {
        var response = await SendAsync(
            HttpMethod.Post, "/api/auth/login", content: JsonContent.Create(new { email, password }), client: client);
        response.EnsureSuccessStatusCode();
        return await LoginResult.FromAsync(response);
    }

    protected async Task<LoginResult> LoginAsNewUserAsync(UserRole role) => await LoginAsync(await CreateUserAsync(role));

    protected Task<HttpResponseMessage> RefreshAsync(string? cookie, string? clientIp = null) =>
        SendAsync(HttpMethod.Post, "/api/auth/refresh", clientIp, refreshCookie: cookie);

    /// <summary>Value of the rt cookie set by the response, or null.</summary>
    protected static string? GetRefreshCookie(HttpResponseMessage response) =>
        GetRefreshSetCookieHeader(response)?.Split(';')[0]["rt=".Length..] is { Length: > 0 } value ? value : null;

    protected static string? GetRefreshSetCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("rt=", StringComparison.Ordinal))
            : null;

    public sealed record LoginResult(LoginResponseDto Body, string RefreshCookie)
    {
        public string AccessToken => Body.AccessToken;

        public static async Task<LoginResult> FromAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions)
                ?? throw new InvalidOperationException("Respuesta de login vacía.");
            var cookie = GetRefreshCookie(response)
                ?? throw new InvalidOperationException("La respuesta no contiene la cookie rt.");
            return new LoginResult(body, cookie);
        }
    }
}

public record UserResponseDto(Guid Id, string Email, string Nombre, string Rol);

public record LoginResponseDto(string AccessToken, int ExpiresIn, UserResponseDto User);

public record ProblemDetailsDto(string? Title, string? Detail, int? Status);

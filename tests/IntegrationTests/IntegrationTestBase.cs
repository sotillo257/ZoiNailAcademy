using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests;

[Collection("IntegrationTests")]
public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static int _ipCounter;

    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Each call returns a fresh fake client IP (RFC 5737 TEST-NET-3 range) so tests
    /// exercising rate-limited /auth/* endpoints don't share the rate limiter's partition
    /// with unrelated tests running against the same shared WebApplicationFactory.
    /// </summary>
    protected static string NextTestClientIp() => $"203.0.113.{Interlocked.Increment(ref _ipCounter) % 254 + 1}";

    protected async Task<HttpResponseMessage> PostAsJsonFromIpAsync<T>(string url, T payload, string clientIp)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-Forwarded-For", clientIp);
        return await Client.SendAsync(request);
    }
}

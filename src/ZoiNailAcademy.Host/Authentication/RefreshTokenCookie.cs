using Microsoft.Extensions.Options;

namespace ZoiNailAcademy.Host.Authentication;

public class AuthCookieOptions
{
    public const string SectionName = "Auth";

    /// <summary>Secure attribute of the refresh cookie. Only set to false for local http development.</summary>
    public bool CookieSecure { get; set; } = true;
}

/// <summary>
/// Reads and writes the refresh token cookie <c>rt</c>. <c>Path=/api/auth</c> limits it to the auth
/// endpoints (including cambiar-password, which needs it to keep the current session).
/// </summary>
public class RefreshTokenCookie(IOptions<AuthCookieOptions> options)
{
    public const string Name = "rt";
    public const string Path = "/api/auth";

    public string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    public void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, refreshToken, BuildOptions(expiresAt));

    public void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, BuildOptions(expires: null));

    private CookieOptions BuildOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = options.Value.CookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}

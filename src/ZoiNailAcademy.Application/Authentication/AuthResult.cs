namespace ZoiNailAcademy.Application.Authentication;

/// <summary>
/// Result of a successful login or refresh. The raw refresh token never goes in a response body:
/// the Host writes it to the <c>rt</c> cookie and returns only <see cref="LoginResponse"/>.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User)
{
    public LoginResponse ToResponse() => new(AccessToken, ExpiresIn, User);
}

/// <summary>Body of login and refresh responses: <c>{accessToken, expiresIn, user}</c>.</summary>
public sealed record LoginResponse(string AccessToken, int ExpiresIn, UserDto User);

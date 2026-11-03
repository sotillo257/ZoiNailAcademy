using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Authentication;

/// <summary>
/// Issues an access token and a new refresh token for a user. The refresh token is added to the
/// context but not saved: the calling handler commits it together with the rest of its changes.
/// </summary>
public class AuthTokenIssuer(
    IAppDbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthTokenSettings tokenSettings,
    TimeProvider timeProvider)
{
    public (AuthResult Result, string RefreshTokenHash) Issue(User user)
    {
        var accessToken = jwtTokenGenerator.Generate(user);

        var rawRefreshToken = refreshTokenHasher.GenerateRawToken();
        var refreshTokenHash = refreshTokenHasher.Hash(rawRefreshToken);
        var expiresAt = timeProvider.GetUtcNow().Add(tokenSettings.RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(RefreshToken.Create(user.Id, refreshTokenHash, expiresAt));

        var result = new AuthResult(
            accessToken,
            (int)tokenSettings.AccessTokenLifetime.TotalSeconds,
            rawRefreshToken,
            expiresAt,
            UserDto.From(user));

        return (result, refreshTokenHash);
    }
}

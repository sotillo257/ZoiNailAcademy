using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Refresh;

public class RefreshHandler(
    IAppDbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<RefreshCommand, Result<TokenPairResponse>>
{
    private const string InvalidTokenMessage = "El refresh token no es válido o ha expirado.";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<TokenPairResponse>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = refreshTokenHasher.Hash(request.RefreshToken);

        var existingToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == presentedHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var rawNewRefreshToken = refreshTokenHasher.GenerateRawToken();
        var newTokenHash = refreshTokenHasher.Hash(rawNewRefreshToken);

        existingToken.Revoke(newTokenHash);

        var newRefreshToken = RefreshToken.Create(user.Id, newTokenHash, DateTime.UtcNow.Add(RefreshTokenLifetime));
        dbContext.RefreshTokens.Add(newRefreshToken);

        var accessToken = jwtTokenGenerator.Generate(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenPairResponse(accessToken, rawNewRefreshToken));
    }
}

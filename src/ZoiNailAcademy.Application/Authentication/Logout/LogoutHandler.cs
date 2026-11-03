using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Logout;

/// <summary>Revokes the presented refresh token. Idempotent: an unknown or missing token is not an error.</summary>
public class LogoutHandler(
    IAppDbContext dbContext,
    IRefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Result.Success();
        }

        var hash = refreshTokenHasher.Hash(request.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

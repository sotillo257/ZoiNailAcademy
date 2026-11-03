using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Refresh;

public class RefreshHandler(
    IAppDbContext dbContext,
    IRefreshTokenHasher refreshTokenHasher,
    AuthTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
    : IRequestHandler<RefreshCommand, Result<AuthResult>>
{
    public const string InvalidTokenMessage = "La sesión no es válida o ha expirado.";

    public async Task<Result<AuthResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var now = timeProvider.GetUtcNow();
        var presentedHash = refreshTokenHasher.Hash(request.RefreshToken);

        var existingToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == presentedHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (existingToken.WasRotated)
        {
            // A rotated token is being replayed: either the client or an attacker holds a stolen
            // copy. We cannot tell which, so every session of the user is closed.
            await RevokeAllActiveTokensAsync(existingToken.UserId, now, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (!existingToken.IsActiveAt(now))
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        var (result, newTokenHash) = tokenIssuer.Issue(user);
        existingToken.Revoke(now, newTokenHash);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request rotated the same token at the same time (xmin changed): only one wins.
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        return Result.Success(result);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }
    }
}

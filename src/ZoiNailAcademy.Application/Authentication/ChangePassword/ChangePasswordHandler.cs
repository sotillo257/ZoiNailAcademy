using FluentValidation.Results;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.ChangePassword;

public class ChangePasswordHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    IRefreshTokenHasher refreshTokenHasher,
    TimeProvider timeProvider)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public const string WrongCurrentPasswordMessage = "La contraseña actual no es correcta.";

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("Usuario no autenticado.");

        // 400 rather than 401: a 401 would make the front treat it as an expired session.
        if (!passwordHasher.Verify(request.PasswordActual, user.PasswordHash))
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(ChangePasswordCommand.PasswordActual), WrongCurrentPasswordMessage)]);
        }

        user.UpdatePasswordHash(passwordHasher.Hash(request.PasswordNueva));

        var currentTokenHash = string.IsNullOrEmpty(request.CurrentRefreshToken)
            ? null
            : refreshTokenHasher.Hash(request.CurrentRefreshToken);

        var otherActiveTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.TokenHash != currentTokenHash)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        foreach (var token in otherActiveTokens)
        {
            token.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

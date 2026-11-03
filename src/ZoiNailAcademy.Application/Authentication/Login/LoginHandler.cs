using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Login;

public class LoginHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private const string GenericErrorMessage = "Email o contraseña incorrectos.";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(GenericErrorMessage);
        }

        var accessToken = jwtTokenGenerator.Generate(user);
        var rawRefreshToken = refreshTokenHasher.GenerateRawToken();
        var refreshTokenHash = refreshTokenHasher.Hash(rawRefreshToken);
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, DateTime.UtcNow.Add(RefreshTokenLifetime));
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            accessToken,
            rawRefreshToken,
            new UserDto(user.Id, user.Email, user.DisplayName));

        return Result.Success(response);
    }
}

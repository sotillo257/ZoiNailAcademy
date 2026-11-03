using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Register;

public class RegisterHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenHasher refreshTokenHasher)
    : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("El email ya está registrado.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(normalizedEmail, request.DisplayName, passwordHash);
        dbContext.Users.Add(user);

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

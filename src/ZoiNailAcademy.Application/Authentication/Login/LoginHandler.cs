using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ZoiNailAcademy.Application.Authentication.Login;

public class LoginHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    public const string GenericErrorMessage = "Email o contraseña incorrectos.";

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(GenericErrorMessage);
        }

        var (result, _) = tokenIssuer.Issue(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result);
    }
}

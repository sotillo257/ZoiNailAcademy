using ZoiNailAcademy.Application.Authentication;
using ZoiNailAcademy.Application.Authentication.ChangePassword;
using ZoiNailAcademy.Application.Authentication.GetCurrentUser;
using ZoiNailAcademy.Application.Authentication.Login;
using ZoiNailAcademy.Application.Authentication.Logout;
using ZoiNailAcademy.Application.Authentication.Refresh;
using ZoiNailAcademy.Host.Authentication;
using ZoiNailAcademy.Host.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ZoiNailAcademy.Host.Controllers;

[ApiController]
[Route("auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController(ISender sender, RefreshTokenCookie refreshTokenCookie) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return SignedIn(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshCommand(refreshTokenCookie.Read(Request)), cancellationToken);
        return SignedIn(result.Value);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(refreshTokenCookie.Read(Request)), cancellationToken);
        refreshTokenCookie.Delete(Response);
        return NoContent();
    }

    // Exempt from the auth rate limit: it requires a valid token and the front calls it on every load.
    [HttpGet("me")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(
            request.PasswordActual, request.PasswordNueva, refreshTokenCookie.Read(Request));
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    private OkObjectResult SignedIn(AuthResult auth)
    {
        refreshTokenCookie.Write(Response, auth.RefreshToken, auth.RefreshTokenExpiresAt);
        return Ok(auth.ToResponse());
    }
}

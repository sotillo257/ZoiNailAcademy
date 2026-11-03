using System.Security.Claims;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Host.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;

        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier) ?? user?.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedException("Usuario no autenticado.");
        }

        return userId;
    }
}

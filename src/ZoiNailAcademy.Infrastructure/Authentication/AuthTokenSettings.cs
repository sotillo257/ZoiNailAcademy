using ZoiNailAcademy.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ZoiNailAcademy.Infrastructure.Authentication;

public class AuthTokenSettings(IOptions<JwtOptions> options) : IAuthTokenSettings
{
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(options.Value.AccessTokenMinutes);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(options.Value.RefreshTokenDays);
}

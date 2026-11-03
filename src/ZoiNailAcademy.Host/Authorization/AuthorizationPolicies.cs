using Microsoft.AspNetCore.Authorization;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Host.Authorization;

public static class AuthorizationPolicies
{
    /// <summary>Client and developer. Every controller under <c>/api/admin</c> requires it at class level.</summary>
    public const string Backoffice = "Backoffice";

    /// <summary>Developer only (course CRUD, email previews).</summary>
    public const string SuperAdmin = "SuperAdmin";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(Backoffice, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.Admin), nameof(UserRole.SuperAdmin)));

        options.AddPolicy(SuperAdmin, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.SuperAdmin)));
    }
}

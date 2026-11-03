using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Application.Authentication;

/// <summary>Authenticated user as exposed by the API contract (<c>{id, email, nombre, rol}</c>).</summary>
public sealed record UserDto(Guid Id, string Email, string Nombre, UserRole Rol)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.DisplayName, user.Role);
}

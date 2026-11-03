using ZoiNailAcademy.Domain.Common;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Enums;

namespace ZoiNailAcademy.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    private User()
    {
    }

    private User(string email, string displayName, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(string email, string displayName, string passwordHash, UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException("El rol del usuario no es válido.");
        }

        return new User(email, displayName, passwordHash, role);
    }

    public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}

using ZoiNailAcademy.Domain.Common;

namespace ZoiNailAcademy.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    private User()
    {
    }

    private User(string email, string displayName, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
    }

    public static User Create(string email, string displayName, string passwordHash)
        => new(email, displayName, passwordHash);

    public void UpdatePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}

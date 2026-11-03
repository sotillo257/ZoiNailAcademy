using ZoiNailAcademy.Domain.Common;

namespace ZoiNailAcademy.Domain.Entities;

public class RefreshToken : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        => new(userId, tokenHash, expiresAt);

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

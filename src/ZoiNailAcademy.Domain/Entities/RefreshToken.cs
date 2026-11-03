using ZoiNailAcademy.Domain.Common;

namespace ZoiNailAcademy.Domain.Entities;

public class RefreshToken : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>True when the token was revoked because it was exchanged for a new one (refresh rotation).</summary>
    public bool WasRotated => ReplacedByTokenHash is not null;

    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new(userId, tokenHash, expiresAt);

    public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;

    /// <summary>Revokes the token. A token that is already revoked keeps its original revocation data.</summary>
    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

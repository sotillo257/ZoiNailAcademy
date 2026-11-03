using FluentAssertions;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActiveAt_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.IsActiveAt(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_AfterRevoke_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.Revoke(Now);

        token.IsActiveAt(Now).Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_AtOrAfterExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now);

        token.IsActiveAt(Now).Should().BeFalse();
        token.IsActiveAt(Now.AddSeconds(-1)).Should().BeTrue();
    }

    [Fact]
    public void Revoke_WithReplacement_MarksAsRotated()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.Revoke(Now, "new-token-hash");

        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenHash.Should().Be("new-token-hash");
        token.WasRotated.Should().BeTrue();
    }

    [Fact]
    public void Revoke_WithoutReplacement_IsNotRotated()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.Revoke(Now);

        token.RevokedAt.Should().Be(Now);
        token.WasRotated.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_KeepsOriginalRevocation()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(14));
        token.Revoke(Now, "first-replacement");

        token.Revoke(Now.AddMinutes(5));

        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenHash.Should().Be("first-replacement");
    }
}

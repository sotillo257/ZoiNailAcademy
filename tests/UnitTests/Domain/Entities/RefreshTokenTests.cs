using FluentAssertions;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Domain.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(30));

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(30));

        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithReplacedByTokenHash_SetsRevokedAtAndReplacement()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(30));

        token.Revoke("new-token-hash");

        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByTokenHash.Should().Be("new-token-hash");
    }

    [Fact]
    public void Revoke_WithoutReplacedByTokenHash_SetsRevokedAtOnly()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(30));

        token.Revoke();

        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByTokenHash.Should().BeNull();
    }
}

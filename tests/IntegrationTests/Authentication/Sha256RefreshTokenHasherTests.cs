using FluentAssertions;
using ZoiNailAcademy.Infrastructure.Authentication;

namespace IntegrationTests.Authentication;

public class Sha256RefreshTokenHasherTests
{
    private readonly Sha256RefreshTokenHasher _hasher = new();

    [Fact]
    public void Hash_WithSameRawToken_ReturnsSameHash()
    {
        var rawToken = _hasher.GenerateRawToken();

        var hash1 = _hasher.Hash(rawToken);
        var hash2 = _hasher.Hash(rawToken);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_WithDifferentRawTokens_ReturnsDifferentHashes()
    {
        var token1 = _hasher.GenerateRawToken();
        var token2 = _hasher.GenerateRawToken();

        var hash1 = _hasher.Hash(token1);
        var hash2 = _hasher.Hash(token2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateRawToken_ReturnsNonEmptyHighEntropyValue()
    {
        var token1 = _hasher.GenerateRawToken();
        var token2 = _hasher.GenerateRawToken();

        token1.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }
}

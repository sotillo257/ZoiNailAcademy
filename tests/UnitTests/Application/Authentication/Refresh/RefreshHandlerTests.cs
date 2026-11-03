using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Authentication.Refresh;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Authentication.Refresh;

public class RefreshHandlerTests
{
    private readonly AuthTestContext _ctx = new();
    private readonly RefreshHandler _handler;
    private readonly User _user = User.Create("clienta@mail.com", "Clienta", "hash", UserRole.Admin);

    public RefreshHandlerTests()
    {
        _ctx.SetupUsers(_user);
        _ctx.JwtTokenGenerator.Setup(x => x.Generate(_user)).Returns("new-access-token");
        _ctx.RefreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-new-token");
        _handler = new RefreshHandler(_ctx.DbContext.Object, _ctx.RefreshTokenHasher.Object, _ctx.CreateIssuer(), _ctx.TimeProvider);
    }

    private RefreshToken TokenFor(string raw, DateTimeOffset? expiresAt = null) =>
        RefreshToken.Create(_user.Id, $"hash:{raw}", expiresAt ?? AuthTestContext.Now.AddDays(10));

    [Fact]
    public async Task Handle_WithActiveToken_RotatesAndReturnsNewSession()
    {
        var existing = TokenFor("raw-old-token");
        _ctx.SetupRefreshTokens(existing);

        var result = await _handler.Handle(new RefreshCommand("raw-old-token"), CancellationToken.None);

        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("raw-new-token");
        result.Value.User.Rol.Should().Be(UserRole.Admin);
        existing.IsActiveAt(AuthTestContext.Now).Should().BeFalse();
        existing.ReplacedByTokenHash.Should().Be("hash:raw-new-token");
        _ctx.RefreshTokens.Should().Contain(t => t.TokenHash == "hash:raw-new-token");
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_WithoutToken_ThrowsUnauthorizedException(string? token)
    {
        var act = async () => await _handler.Handle(new RefreshCommand(token), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsUnauthorizedException()
    {
        var act = async () => await _handler.Handle(new RefreshCommand("does-not-exist"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsUnauthorizedException()
    {
        _ctx.SetupRefreshTokens(TokenFor("raw-old-token", AuthTestContext.Now.AddSeconds(-1)));

        var act = async () => await _handler.Handle(new RefreshCommand("raw-old-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTokenRevokedByLogout_ThrowsWithoutRevokingOtherSessions()
    {
        var loggedOut = TokenFor("raw-old-token");
        loggedOut.Revoke(AuthTestContext.Now);
        var otherSession = TokenFor("raw-other");
        _ctx.SetupRefreshTokens(loggedOut, otherSession);

        var act = async () => await _handler.Handle(new RefreshCommand("raw-old-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        otherSession.IsActiveAt(AuthTestContext.Now).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReusingRotatedToken_RevokesEveryActiveTokenOfTheUser()
    {
        var rotated = TokenFor("raw-old-token");
        rotated.Revoke(AuthTestContext.Now, "hash:raw-successor");
        var successor = TokenFor("raw-successor");
        var otherSession = TokenFor("raw-other");
        var otherUsersToken = RefreshToken.Create(Guid.NewGuid(), "hash:foreign", AuthTestContext.Now.AddDays(10));
        _ctx.SetupRefreshTokens(rotated, successor, otherSession, otherUsersToken);

        var act = async () => await _handler.Handle(new RefreshCommand("raw-old-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        successor.IsActiveAt(AuthTestContext.Now).Should().BeFalse();
        otherSession.IsActiveAt(AuthTestContext.Now).Should().BeFalse();
        otherUsersToken.IsActiveAt(AuthTestContext.Now).Should().BeTrue();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

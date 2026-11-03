using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.Refresh;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Authentication.Refresh;

public class RefreshHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasher = new();
    private readonly RefreshHandler _handler;

    public RefreshHandlerTests()
    {
        _handler = new RefreshHandler(_dbContext.Object, _jwtTokenGenerator.Object, _refreshTokenHasher.Object);
    }

    private void SetupUsers(params User[] users)
    {
        var mockSet = users.ToList().BuildMockDbSet();
        _dbContext.Setup(x => x.Users).Returns(mockSet.Object);
    }

    private void SetupRefreshTokens(params RefreshToken[] tokens)
    {
        var mockSet = tokens.ToList().BuildMockDbSet();
        _dbContext.Setup(x => x.RefreshTokens).Returns(mockSet.Object);
    }

    [Fact]
    public async Task Handle_WithActiveToken_RotatesAndReturnsNewPair()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "hash");
        var existingToken = RefreshToken.Create(user.Id, "old-hash", DateTime.UtcNow.AddDays(10));
        SetupUsers(user);
        SetupRefreshTokens(existingToken);
        _refreshTokenHasher.Setup(x => x.Hash("raw-old-token")).Returns("old-hash");
        _refreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-new-token");
        _refreshTokenHasher.Setup(x => x.Hash("raw-new-token")).Returns("new-hash");
        _jwtTokenGenerator.Setup(x => x.Generate(user)).Returns("new-access-token");

        var command = new RefreshCommand("raw-old-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("raw-new-token");
        existingToken.IsActive.Should().BeFalse();
        existingToken.ReplacedByTokenHash.Should().Be("new-hash");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAlreadyRotatedToken_ThrowsUnauthorizedException()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "hash");
        var revokedToken = RefreshToken.Create(user.Id, "old-hash", DateTime.UtcNow.AddDays(10));
        revokedToken.Revoke("some-other-hash");
        SetupUsers(user);
        SetupRefreshTokens(revokedToken);
        _refreshTokenHasher.Setup(x => x.Hash("raw-old-token")).Returns("old-hash");

        var command = new RefreshCommand("raw-old-token");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsUnauthorizedException()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "hash");
        var expiredToken = RefreshToken.Create(user.Id, "old-hash", DateTime.UtcNow.AddSeconds(-1));
        SetupUsers(user);
        SetupRefreshTokens(expiredToken);
        _refreshTokenHasher.Setup(x => x.Hash("raw-old-token")).Returns("old-hash");

        var command = new RefreshCommand("raw-old-token");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsUnauthorizedException()
    {
        SetupUsers();
        SetupRefreshTokens();
        _refreshTokenHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("unknown-hash");

        var command = new RefreshCommand("does-not-exist");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

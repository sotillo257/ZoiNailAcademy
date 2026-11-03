using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Authentication.Logout;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Application.Authentication.Logout;

public class LogoutHandlerTests
{
    private readonly AuthTestContext _ctx = new();
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _handler = new LogoutHandler(_ctx.DbContext.Object, _ctx.RefreshTokenHasher.Object, _ctx.TimeProvider);
    }

    [Fact]
    public async Task Handle_WithActiveToken_RevokesIt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash:raw", AuthTestContext.Now.AddDays(1));
        _ctx.SetupRefreshTokens(token);

        var result = await _handler.Handle(new LogoutCommand("raw"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().Be(AuthTestContext.Now);
        token.WasRotated.Should().BeFalse();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    public async Task Handle_WithoutKnownToken_SucceedsWithoutSaving(string? rawToken)
    {
        var result = await _handler.Handle(new LogoutCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

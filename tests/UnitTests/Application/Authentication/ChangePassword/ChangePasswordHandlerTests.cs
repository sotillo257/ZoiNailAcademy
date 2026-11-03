using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Authentication.ChangePassword;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Authentication.ChangePassword;

public class ChangePasswordHandlerTests
{
    private readonly AuthTestContext _ctx = new();
    private readonly ChangePasswordHandler _handler;
    private readonly User _user = User.Create("clienta@mail.com", "Clienta", "old-hash", UserRole.Admin);

    public ChangePasswordHandlerTests()
    {
        _ctx.SetupUsers(_user);
        _ctx.CurrentUser.Setup(x => x.GetUserId()).Returns(_user.Id);
        _ctx.PasswordHasher.Setup(x => x.Verify("actual-pass", "old-hash")).Returns(true);
        _ctx.PasswordHasher.Setup(x => x.Hash("new-password-123")).Returns("new-hash");
        _handler = new ChangePasswordHandler(
            _ctx.DbContext.Object, _ctx.CurrentUser.Object, _ctx.PasswordHasher.Object,
            _ctx.RefreshTokenHasher.Object, _ctx.TimeProvider);
    }

    [Fact]
    public async Task Handle_WithCorrectCurrentPassword_UpdatesHashAndRevokesOtherSessions()
    {
        var current = RefreshToken.Create(_user.Id, "hash:current", AuthTestContext.Now.AddDays(1));
        var other = RefreshToken.Create(_user.Id, "hash:other", AuthTestContext.Now.AddDays(1));
        var foreign = RefreshToken.Create(Guid.NewGuid(), "hash:foreign", AuthTestContext.Now.AddDays(1));
        _ctx.SetupRefreshTokens(current, other, foreign);

        var result = await _handler.Handle(
            new ChangePasswordCommand("actual-pass", "new-password-123", "current"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _user.PasswordHash.Should().Be("new-hash");
        current.IsActiveAt(AuthTestContext.Now).Should().BeTrue();
        other.IsActiveAt(AuthTestContext.Now).Should().BeFalse();
        foreign.IsActiveAt(AuthTestContext.Now).Should().BeTrue();
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutCurrentCookie_RevokesEverySession()
    {
        var other = RefreshToken.Create(_user.Id, "hash:other", AuthTestContext.Now.AddDays(1));
        _ctx.SetupRefreshTokens(other);

        await _handler.Handle(new ChangePasswordCommand("actual-pass", "new-password-123", null), CancellationToken.None);

        other.IsActiveAt(AuthTestContext.Now).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsValidationExceptionAndKeepsHash()
    {
        var act = async () => await _handler.Handle(
            new ChangePasswordCommand("wrong-pass", "new-password-123", null), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey(nameof(ChangePasswordCommand.PasswordActual));
        _user.PasswordHash.Should().Be("old-hash");
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

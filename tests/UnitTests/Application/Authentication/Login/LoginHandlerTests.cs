using FluentAssertions;
using Moq;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Authentication.Login;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Authentication.Login;

public class LoginHandlerTests
{
    private readonly AuthTestContext _ctx = new();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(_ctx.DbContext.Object, _ctx.PasswordHasher.Object, _ctx.CreateIssuer());
    }

    private User SetupValidUser()
    {
        var user = User.Create("clienta@mail.com", "Clienta", "stored-hash", UserRole.Admin);
        _ctx.SetupUsers(user);
        _ctx.PasswordHasher.Setup(x => x.Verify("password123", "stored-hash")).Returns(true);
        _ctx.JwtTokenGenerator.Setup(x => x.Generate(user)).Returns("access-token");
        _ctx.RefreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-refresh-token");
        return user;
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsTokensUserAndRole()
    {
        var user = SetupValidUser();

        var result = await _handler.Handle(new LoginCommand("Clienta@Mail.com", "password123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.ExpiresIn.Should().Be(15 * 60);
        result.Value.RefreshToken.Should().Be("raw-refresh-token");
        result.Value.RefreshTokenExpiresAt.Should().Be(AuthTestContext.Now.AddDays(14));
        result.Value.User.Should().BeEquivalentTo(new { user.Id, Email = "clienta@mail.com", Nombre = "Clienta", Rol = UserRole.Admin });
        _ctx.DbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_PersistsOnlyTheHashOfTheRefreshToken()
    {
        var user = SetupValidUser();

        await _handler.Handle(new LoginCommand("clienta@mail.com", "password123"), CancellationToken.None);

        _ctx.RefreshTokens.Should().ContainSingle()
            .Which.Should().Match<RefreshToken>(t =>
                t.UserId == user.Id && t.TokenHash == "hash:raw-refresh-token" && t.ExpiresAt == AuthTestContext.Now.AddDays(14));
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsGenericUnauthorizedException()
    {
        var act = async () => await _handler.Handle(new LoginCommand("unknown@mail.com", "password123"), CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage(LoginHandler.GenericErrorMessage);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsGenericUnauthorizedException()
    {
        SetupValidUser();

        var act = async () => await _handler.Handle(new LoginCommand("clienta@mail.com", "wrong-password"), CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage(LoginHandler.GenericErrorMessage);
        _ctx.RefreshTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NormalizesEmail_BeforeLookup()
    {
        SetupValidUser();

        var result = await _handler.Handle(new LoginCommand("  CLIENTA@MAIL.COM  ", "password123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

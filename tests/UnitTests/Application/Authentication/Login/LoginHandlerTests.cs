using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.Login;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Authentication.Login;

public class LoginHandlerTests
{
    private const string GenericErrorMessage = "Email o contraseña incorrectos.";

    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasher = new();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(
            _dbContext.Object,
            _passwordHasher.Object,
            _jwtTokenGenerator.Object,
            _refreshTokenHasher.Object);
    }

    private void SetupUsers(params User[] existingUsers)
    {
        var mockSet = existingUsers.ToList().BuildMockDbSet();
        _dbContext.Setup(x => x.Users).Returns(mockSet.Object);
    }

    private void SetupRefreshTokens()
    {
        var mockSet = new List<RefreshToken>().BuildMockDbSet();
        _dbContext.Setup(x => x.RefreshTokens).Returns(mockSet.Object);
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsTokensAndUser()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "stored-hash");
        SetupUsers(user);
        SetupRefreshTokens();
        _passwordHasher.Setup(x => x.Verify("password123", "stored-hash")).Returns(true);
        _jwtTokenGenerator.Setup(x => x.Generate(user)).Returns("access-token");
        _refreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-refresh-token");
        _refreshTokenHasher.Setup(x => x.Hash("raw-refresh-token")).Returns("hashed-refresh-token");

        var command = new LoginCommand("Jesus@Mail.com", "password123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("raw-refresh-token");
        result.Value.User.Email.Should().Be("jesus@mail.com");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsGenericUnauthorizedException()
    {
        SetupUsers();
        SetupRefreshTokens();

        var command = new LoginCommand("unknown@mail.com", "password123");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage(GenericErrorMessage);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsGenericUnauthorizedException()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "stored-hash");
        SetupUsers(user);
        SetupRefreshTokens();
        _passwordHasher.Setup(x => x.Verify("wrong-password", "stored-hash")).Returns(false);

        var command = new LoginCommand("jesus@mail.com", "wrong-password");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage(GenericErrorMessage);
    }

    [Fact]
    public async Task Handle_NormalizesEmail_BeforeLookup()
    {
        var user = User.Create("jesus@mail.com", "Jesús", "stored-hash");
        SetupUsers(user);
        SetupRefreshTokens();
        _passwordHasher.Setup(x => x.Verify("password123", "stored-hash")).Returns(true);
        _jwtTokenGenerator.Setup(x => x.Generate(user)).Returns("access-token");
        _refreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-refresh-token");
        _refreshTokenHasher.Setup(x => x.Hash("raw-refresh-token")).Returns("hashed-refresh-token");

        var command = new LoginCommand("  JESUS@MAIL.COM  ", "password123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

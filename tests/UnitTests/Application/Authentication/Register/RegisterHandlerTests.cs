using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.Register;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace UnitTests.Application.Authentication.Register;

public class RegisterHandlerTests
{
    private readonly Mock<IAppDbContext> _dbContext = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasher = new();
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _handler = new RegisterHandler(
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
    public async Task Handle_WithNewEmail_CreatesUserAndReturnsTokens()
    {
        SetupUsers();
        SetupRefreshTokens();
        _passwordHasher.Setup(x => x.Hash("password123")).Returns("hashed-password");
        _jwtTokenGenerator.Setup(x => x.Generate(It.IsAny<User>())).Returns("access-token");
        _refreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-refresh-token");
        _refreshTokenHasher.Setup(x => x.Hash("raw-refresh-token")).Returns("hashed-refresh-token");

        var command = new RegisterCommand("Jesus@Mail.com", "password123", "Jesús");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("raw-refresh-token");
        result.Value.User.Email.Should().Be("jesus@mail.com");
        result.Value.User.DisplayName.Should().Be("Jesús");
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NormalizesEmail_BeforeDuplicateCheck()
    {
        var existingUser = User.Create("jesus@mail.com", "Jesús", "existing-hash");
        SetupUsers(existingUser);
        SetupRefreshTokens();

        var command = new RegisterCommand("  Jesus@Mail.com  ", "password123", "Otro");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ThrowsConflictException()
    {
        var existingUser = User.Create("jesus@mail.com", "Jesús", "existing-hash");
        SetupUsers(existingUser);
        SetupRefreshTokens();

        var command = new RegisterCommand("jesus@mail.com", "password123", "Jesús");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StoresHashedPassword_NeverPlaintext()
    {
        SetupUsers();
        SetupRefreshTokens();
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtTokenGenerator.Setup(x => x.Generate(It.IsAny<User>())).Returns("access-token");
        _refreshTokenHasher.Setup(x => x.GenerateRawToken()).Returns("raw-refresh-token");
        _refreshTokenHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-refresh-token");

        User? addedUser = null;
        _dbContext.Setup(x => x.Users.Add(It.IsAny<User>()))
            .Callback<User>(u => addedUser = u);

        var command = new RegisterCommand("jesus@mail.com", "password123", "Jesús");

        await _handler.Handle(command, CancellationToken.None);

        addedUser.Should().NotBeNull();
        addedUser!.PasswordHash.Should().Be("hashed-password");
        addedUser.PasswordHash.Should().NotBe("password123");
    }
}

using Microsoft.Extensions.Time.Testing;
using MockQueryable.Moq;
using Moq;
using ZoiNailAcademy.Application.Authentication;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.TestSupport;

/// <summary>Mocks shared by the authentication handler tests.</summary>
public class AuthTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    public Mock<IAppDbContext> DbContext { get; } = new();
    public Mock<IPasswordHasher> PasswordHasher { get; } = new();
    public Mock<IJwtTokenGenerator> JwtTokenGenerator { get; } = new();
    public Mock<IRefreshTokenHasher> RefreshTokenHasher { get; } = new();
    public Mock<IAuthTokenSettings> TokenSettings { get; } = new();
    public Mock<ICurrentUserService> CurrentUser { get; } = new();
    public FakeTimeProvider TimeProvider { get; } = new(Now);

    public List<RefreshToken> RefreshTokens { get; private set; } = [];

    public AuthTestContext()
    {
        TokenSettings.Setup(x => x.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        TokenSettings.Setup(x => x.RefreshTokenLifetime).Returns(TimeSpan.FromDays(14));
        RefreshTokenHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(raw => $"hash:{raw}");
        SetupUsers();
        SetupRefreshTokens();
    }

    public AuthTokenIssuer CreateIssuer() => new(
        DbContext.Object, JwtTokenGenerator.Object, RefreshTokenHasher.Object, TokenSettings.Object, TimeProvider);

    public void SetupUsers(params User[] users)
    {
        var mockSet = users.ToList().BuildMockDbSet();
        DbContext.Setup(x => x.Users).Returns(mockSet.Object);
    }

    public void SetupRefreshTokens(params RefreshToken[] tokens)
    {
        RefreshTokens = tokens.ToList();
        var mockSet = RefreshTokens.BuildMockDbSet();
        mockSet.Setup(x => x.Add(It.IsAny<RefreshToken>())).Callback<RefreshToken>(RefreshTokens.Add);
        DbContext.Setup(x => x.RefreshTokens).Returns(mockSet.Object);
    }
}

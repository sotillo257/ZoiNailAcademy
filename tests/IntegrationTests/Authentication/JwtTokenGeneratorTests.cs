using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Infrastructure.Authentication;

namespace IntegrationTests.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        SigningKey = "unit-test-signing-key-with-enough-entropy-1234567890",
        Issuer = "ZoiNailAcademy.Api",
        Audience = "ZoiNailAcademy.Client",
        AccessTokenMinutes = 15
    };

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public void Generate_IssuesTokenWithSubjectAndRoleClaims(UserRole role)
    {
        var generator = new JwtTokenGenerator(Options.Create(_options), _clock);
        var user = User.Create("clienta@mail.com", "Clienta", "hash", role);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(generator.Generate(user));

        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().ContainSingle(c => c.Type == "role").Which.Value.Should().Be(role.ToString());
    }

    [Fact]
    public void Generate_ExpiresAccessTokenMinutesAfterTimeProviderNow()
    {
        var generator = new JwtTokenGenerator(Options.Create(_options), _clock);
        var user = User.Create("clienta@mail.com", "Clienta", "hash", UserRole.Admin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(generator.Generate(user));

        jwt.ValidTo.Should().Be(_clock.GetUtcNow().AddMinutes(15).UtcDateTime);
    }
}

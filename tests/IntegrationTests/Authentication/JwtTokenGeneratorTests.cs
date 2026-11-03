using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

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

    [Fact]
    public void Generate_IssuesTokenWithUserIdAsSubjectClaim()
    {
        var generator = new JwtTokenGenerator(Options.Create(_options));
        var user = User.Create("jesus@mail.com", "Jesús", "hash");

        var token = generator.Generate(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void Generate_IssuesTokenExpiringInApproximately15Minutes()
    {
        var generator = new JwtTokenGenerator(Options.Create(_options));
        var user = User.Create("jesus@mail.com", "Jesús", "hash");

        var before = DateTime.UtcNow;
        var token = generator.Generate(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromMinutes(1));
    }
}

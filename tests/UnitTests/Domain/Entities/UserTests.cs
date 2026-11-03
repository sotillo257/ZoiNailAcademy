using FluentAssertions;
using ZoiNailAcademy.Domain.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Domain.Entities;

public class UserTests
{
    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public void Create_WithRole_ExposesRole(UserRole role)
    {
        var user = User.Create("clienta@mail.com", "Clienta", "hash", role);

        user.Role.Should().Be(role);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithUndefinedRole_ThrowsDomainException()
    {
        var act = () => User.Create("clienta@mail.com", "Clienta", "hash", (UserRole)99);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePasswordHash_ReplacesHash()
    {
        var user = User.Create("clienta@mail.com", "Clienta", "old-hash", UserRole.Admin);

        user.UpdatePasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
    }
}

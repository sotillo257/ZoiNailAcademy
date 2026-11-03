using FluentAssertions;
using UnitTests.TestSupport;
using ZoiNailAcademy.Application.Authentication.GetCurrentUser;
using ZoiNailAcademy.Application.Common.Exceptions;
using ZoiNailAcademy.Domain.Entities;
using ZoiNailAcademy.Domain.Enums;

namespace UnitTests.Application.Authentication.GetCurrentUser;

public class GetCurrentUserHandlerTests
{
    private readonly AuthTestContext _ctx = new();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests()
    {
        _handler = new GetCurrentUserHandler(_ctx.DbContext.Object, _ctx.CurrentUser.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsUserDto()
    {
        var user = User.Create("dev@mail.com", "Dev", "hash", UserRole.SuperAdmin);
        _ctx.SetupUsers(user);
        _ctx.CurrentUser.Setup(x => x.GetUserId()).Returns(user.Id);

        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(new { user.Id, Email = "dev@mail.com", Nombre = "Dev", Rol = UserRole.SuperAdmin });
    }

    [Fact]
    public async Task Handle_WithDeletedUser_ThrowsUnauthorizedException()
    {
        _ctx.CurrentUser.Setup(x => x.GetUserId()).Returns(Guid.NewGuid());

        var act = async () => await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.ChangePassword;

namespace UnitTests.Application.Authentication.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Validate_WithValidPayload_HasNoErrors()
    {
        _validator.Validate(new ChangePasswordCommand("actual-pass", "0123456789", null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCurrentPassword_HasError()
    {
        var result = _validator.Validate(new ChangePasswordCommand("", "0123456789", null));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ChangePasswordCommand.PasswordActual));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789")]
    public void Validate_WithNewPasswordShorterThan10_HasError(string newPassword)
    {
        var result = _validator.Validate(new ChangePasswordCommand("actual-pass", newPassword, null));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.PasswordNueva));
    }
}

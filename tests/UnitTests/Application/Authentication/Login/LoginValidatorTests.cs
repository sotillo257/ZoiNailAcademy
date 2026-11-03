using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.Login;

namespace UnitTests.Application.Authentication.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_WithEmailAndPassword_HasNoErrors()
    {
        _validator.Validate(new LoginCommand("clienta@mail.com", "password123")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEmail_HasEmailError()
    {
        var result = _validator.Validate(new LoginCommand("", "password123"));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasPasswordError()
    {
        var result = _validator.Validate(new LoginCommand("clienta@mail.com", ""));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}

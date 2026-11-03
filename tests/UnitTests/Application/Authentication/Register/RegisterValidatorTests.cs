using FluentAssertions;
using ZoiNailAcademy.Application.Authentication.Register;

namespace UnitTests.Application.Authentication.Register;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_WithValidPayload_HasNoErrors()
    {
        var command = new RegisterCommand("jesus@mail.com", "password123", "Jesús");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Validate_WithMalformedEmail_HasError(string email)
    {
        var command = new RegisterCommand(email, "password123", "Jesús");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1")]
    [InlineData("1234567")]
    public void Validate_WithPasswordShorterThan8Characters_HasError(string password)
    {
        var command = new RegisterCommand("jesus@mail.com", password, "Jesús");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithEmptyDisplayName_HasError()
    {
        var command = new RegisterCommand("jesus@mail.com", "password123", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.DisplayName));
    }
}

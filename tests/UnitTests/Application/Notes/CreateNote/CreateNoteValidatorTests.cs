using FluentAssertions;
using ZoiNailAcademy.Application.Notes.CreateNote;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Application.Notes.CreateNote;

public class CreateNoteValidatorTests
{
    private readonly CreateNoteValidator _validator = new();

    [Fact]
    public void Validate_WithValidPayload_HasNoErrors()
    {
        var result = _validator.Validate(new CreateNoteCommand("Ideas", "Contenido"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyContent_HasNoErrors()
    {
        var result = _validator.Validate(new CreateNoteCommand("Ideas", ""));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTitle_HasTitleError()
    {
        var result = _validator.Validate(new CreateNoteCommand("", "Contenido"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNoteCommand.Title));
    }

    [Fact]
    public void Validate_WithTitleTooLong_HasTitleError()
    {
        var result = _validator.Validate(new CreateNoteCommand(new string('a', Note.TitleMaxLength + 1), "Contenido"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNoteCommand.Title));
    }

    [Fact]
    public void Validate_WithContentTooLong_HasContentError()
    {
        var result = _validator.Validate(new CreateNoteCommand("Ideas", new string('a', Note.ContentMaxLength + 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNoteCommand.Content));
    }
}

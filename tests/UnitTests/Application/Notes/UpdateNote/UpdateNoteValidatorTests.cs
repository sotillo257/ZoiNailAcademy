using FluentAssertions;
using ZoiNailAcademy.Application.Notes.UpdateNote;
using ZoiNailAcademy.Domain.Entities;

namespace UnitTests.Application.Notes.UpdateNote;

public class UpdateNoteValidatorTests
{
    private readonly UpdateNoteValidator _validator = new();

    [Fact]
    public void Validate_WithValidPayload_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateNoteCommand(Guid.NewGuid(), "Ideas", "Contenido"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTitle_HasTitleError()
    {
        var result = _validator.Validate(new UpdateNoteCommand(Guid.NewGuid(), "", "Contenido"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNoteCommand.Title));
    }

    [Fact]
    public void Validate_WithContentTooLong_HasContentError()
    {
        var result = _validator.Validate(new UpdateNoteCommand(Guid.NewGuid(), "Ideas", new string('a', Note.ContentMaxLength + 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNoteCommand.Content));
    }
}

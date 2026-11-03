using FluentAssertions;
using ZoiNailAcademy.Application.Notes.GetNotes;

namespace UnitTests.Application.Notes.GetNotes;

public class GetNotesValidatorTests
{
    private readonly GetNotesValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_HasNoErrors()
    {
        var result = _validator.Validate(new GetNotesQuery(1, 20));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageBelowOne_HasPageError(int page)
    {
        var result = _validator.Validate(new GetNotesQuery(page, 20));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetNotesQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetNotesValidator.MaxPageSize + 1)]
    public void Validate_WithPageSizeOutOfRange_HasPageSizeError(int pageSize)
    {
        var result = _validator.Validate(new GetNotesQuery(1, pageSize));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetNotesQuery.PageSize));
    }
}

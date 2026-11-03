using FluentAssertions;
using ZoiNailAcademy.Application.Common;

namespace UnitTests.Application.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = "Algo salió mal";

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessWithValue_ShouldCreateSuccessfulResultWithValue()
    {
        var value = 42;

        var result = Result.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void FailureWithValue_ShouldCreateFailedResultWithDefaultValue()
    {
        var error = "No encontrado";

        var result = Result.Failure<int>(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Value.Should().Be(default(int));
    }

    [Fact]
    public void Constructor_SuccessWithError_ShouldThrowInvalidOperationException()
    {
        var act = () => new TestableResult(true, "error");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Success result cannot have an error.");
    }

    [Fact]
    public void Constructor_FailureWithoutError_ShouldThrowInvalidOperationException()
    {
        var act = () => new TestableResult(false, string.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Failure result must have an error.");
    }

    private class TestableResult : Result
    {
        public TestableResult(bool isSuccess, string error) : base(isSuccess, error)
        {
        }
    }
}

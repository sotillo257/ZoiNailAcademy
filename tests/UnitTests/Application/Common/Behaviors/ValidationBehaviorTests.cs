using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using ZoiNailAcademy.Application.Common;
using ZoiNailAcademy.Application.Common.Behaviors;
using MediatR;
using Moq;
using ValidationException = ZoiNailAcademy.Application.Common.Exceptions.ValidationException;

namespace UnitTests.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldPassThrough()
    {
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, Result>(validators);
        var request = new TestRequest("Valid");
        var expectedResult = Result.Success();

        var result = await behavior.Handle(
            request,
            (ct) => Task.FromResult(expectedResult),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPassingValidation_ShouldPassThrough()
    {
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestRequest, Result>(validators);
        var request = new TestRequest("Valid");

        var result = await behavior.Handle(
            request,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFailingValidation_ShouldThrowValidationException()
    {
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "El nombre es obligatorio")
            }));

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestRequest, Result>(validators);
        var request = new TestRequest("");
        var handlerCalled = false;

        var act = async () => await behavior.Handle(
            request,
            (ct) =>
            {
                handlerCalled = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
        exception.Which.Errors["Name"].Should().Contain("El nombre es obligatorio");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithGenericResult_ShouldThrowValidationException()
    {
        var validatorMock = new Mock<IValidator<TestRequestWithGuid>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequestWithGuid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "El nombre es obligatorio")
            }));

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestRequestWithGuid, Result<Guid>>(validators);
        var request = new TestRequestWithGuid("");

        var act = async () => await behavior.Handle(
            request,
            (ct) => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
    }

    public record TestRequest(string Name) : IRequest<Result>;
    public record TestRequestWithGuid(string Name) : IRequest<Result<Guid>>;
}

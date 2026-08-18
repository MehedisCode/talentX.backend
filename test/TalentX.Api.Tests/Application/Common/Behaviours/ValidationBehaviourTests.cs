using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using TalentX.Application.Common.Behaviours;

namespace TalentX.Api.Tests.Application.Common.Behaviours;

public class ValidationBehaviourTests
{
    public class SampleRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        var nextCalled = false;
        RequestHandlerDelegate<string> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithPassingValidators_CallsNext()
    {
        var validator = new Mock<IValidator<SampleRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behaviour = new ValidationBehaviour<SampleRequest, string>(new[] { validator.Object });
        RequestHandlerDelegate<string> next = (_) => Task.FromResult("ok");

        var result = await behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        var failure = new ValidationFailure("Value", "Value is required.");
        var validator = new Mock<IValidator<SampleRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { failure }));

        var behaviour = new ValidationBehaviour<SampleRequest, string>(new[] { validator.Object });
        var nextCalled = false;
        RequestHandlerDelegate<string> next = (_) =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        var act = () => behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Value is required.");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithMultipleValidatorsWhereOneFails_AggregatesFailuresFromAll()
    {
        var passingValidator = new Mock<IValidator<SampleRequest>>();
        passingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var failingValidator = new Mock<IValidator<SampleRequest>>();
        failingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Invalid.") }));

        var behaviour = new ValidationBehaviour<SampleRequest, string>(
            new[] { passingValidator.Object, failingValidator.Object }
        );
        RequestHandlerDelegate<string> next = (_) => Task.FromResult("ok");

        var act = () => behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle();
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.DeactivateJob;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.DeactivateJob;

public sealed class DeactivateJobCommandValidatorTests
{
    private readonly DeactivateJobCommandValidator _validator = new();

    private static DeactivateJobCommand ValidCommand(
        Guid? jobId = null,
        DeactivationReason reason = DeactivationReason.Spam,
        string details = "Posting links to an unrelated external site.")
    {
        return new DeactivateJobCommand(jobId ?? Guid.NewGuid(), reason, details);
    }

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenJobIdIsEmpty_HasValidationError()
    {
        _validator.TestValidate(ValidCommand(jobId: Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.JobId)
            .WithErrorMessage("Job ID is required.");
    }

    [Fact]
    public void Validate_WhenReasonIsNotADefinedValue_HasValidationError()
    {
        _validator.TestValidate(ValidCommand(reason: (DeactivationReason)999))
            .ShouldHaveValidationErrorFor(command => command.Reason)
            .WithErrorMessage("Invalid deactivation reason.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDetailsAreBlank_HasRequiredError(string blankDetails)
    {
        _validator.TestValidate(ValidCommand(details: blankDetails))
            .ShouldHaveValidationErrorFor(command => command.Details)
            .WithErrorMessage("Please explain why this job is being deactivated.");
    }

    [Fact]
    public void Validate_WhenDetailsAreBlank_ReportsOnlyTheRequiredError()
    {
        // Stop cascade: a blank value must not also trip the minimum-length rule.
        var result = _validator.TestValidate(ValidCommand(details: "   "));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(DeactivateJobCommand.Details));
    }

    [Fact]
    public void Validate_WhenTrimmedDetailsAreShorterThanTenCharacters_HasValidationError()
    {
        // 9 characters once the surrounding whitespace is trimmed.
        _validator.TestValidate(ValidCommand(details: "   123456789   "))
            .ShouldHaveValidationErrorFor(command => command.Details)
            .WithErrorMessage("Please explain the reason in at least 10 characters.");
    }

    [Fact]
    public void Validate_WhenDetailsAreExactlyTenCharacters_HasNoValidationErrors()
    {
        _validator.TestValidate(ValidCommand(details: "1234567890")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDetailsAreExactly1000Characters_HasNoValidationErrors()
    {
        _validator.TestValidate(ValidCommand(details: new string('a', 1000))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDetailsExceed1000Characters_HasValidationError()
    {
        _validator.TestValidate(ValidCommand(details: new string('a', 1001)))
            .ShouldHaveValidationErrorFor(command => command.Details)
            .WithErrorMessage("Details must be 1000 characters or fewer.");
    }
}

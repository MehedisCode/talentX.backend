using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.RejectCompanyReview;

public sealed class RejectCompanyReviewCommandValidatorTests
{
    private readonly RejectCompanyReviewCommandValidator _validator = new();

    private static RejectCompanyReviewCommand ValidCommand(
        Guid? reviewId = null,
        ReviewModerationReason reason = ReviewModerationReason.Spam,
        string details = "This looks like a spam submission.")
    {
        return new RejectCompanyReviewCommand(reviewId ?? Guid.NewGuid(), reason, details);
    }

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_HasValidationError()
    {
        _validator.TestValidate(ValidCommand(reviewId: Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }

    [Fact]
    public void Validate_WhenReasonIsNotADefinedValue_HasValidationError()
    {
        _validator.TestValidate(ValidCommand(reason: (ReviewModerationReason)999))
            .ShouldHaveValidationErrorFor(command => command.Reason)
            .WithErrorMessage("Invalid rejection reason.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDetailsAreBlank_HasRequiredError(string blankDetails)
    {
        _validator.TestValidate(ValidCommand(details: blankDetails))
            .ShouldHaveValidationErrorFor(command => command.Details)
            .WithErrorMessage("Please explain why this review is being rejected.");
    }

    [Fact]
    public void Validate_WhenDetailsAreBlank_ReportsOnlyTheRequiredError()
    {
        var result = _validator.TestValidate(ValidCommand(details: "   "));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(RejectCompanyReviewCommand.Details));
    }

    [Fact]
    public void Validate_WhenTrimmedDetailsAreShorterThanTenCharacters_HasValidationError()
    {
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

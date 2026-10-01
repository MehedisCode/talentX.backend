using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.ApproveCompanyReview;

public sealed class ApproveCompanyReviewCommandValidatorTests
{
    private readonly ApproveCompanyReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        _validator.TestValidate(new ApproveCompanyReviewCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_HasValidationError()
    {
        _validator.TestValidate(new ApproveCompanyReviewCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }
}

using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.UnhideCompanyReview;

public sealed class UnhideCompanyReviewCommandValidatorTests
{
    private readonly UnhideCompanyReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        _validator.TestValidate(new UnhideCompanyReviewCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_HasValidationError()
    {
        _validator.TestValidate(new UnhideCompanyReviewCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }
}

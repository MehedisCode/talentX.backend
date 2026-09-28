using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed class WithdrawCompanyReviewCommandValidatorTests
{
    private readonly WithdrawCompanyReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WithAReviewId_HasNoErrors()
    {
        _validator.TestValidate(new WithdrawCompanyReviewCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_HasError()
    {
        _validator.TestValidate(new WithdrawCompanyReviewCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }
}

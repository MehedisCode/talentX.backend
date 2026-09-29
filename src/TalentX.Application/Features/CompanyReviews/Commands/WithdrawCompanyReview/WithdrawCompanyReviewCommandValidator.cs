using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed class WithdrawCompanyReviewCommandValidator : AbstractValidator<WithdrawCompanyReviewCommand>
{
    public WithdrawCompanyReviewCommandValidator()
    {
        RuleFor(v => v.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");
    }
}

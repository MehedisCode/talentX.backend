using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;

public sealed class UnhideCompanyReviewCommandValidator : AbstractValidator<UnhideCompanyReviewCommand>
{
    public UnhideCompanyReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required.");
    }
}

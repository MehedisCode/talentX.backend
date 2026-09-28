using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;

public sealed class ApproveCompanyReviewCommandValidator : AbstractValidator<ApproveCompanyReviewCommand>
{
    public ApproveCompanyReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required.");
    }
}

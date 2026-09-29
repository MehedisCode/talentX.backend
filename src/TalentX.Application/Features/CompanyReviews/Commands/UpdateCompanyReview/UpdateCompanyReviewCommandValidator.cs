using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;

public sealed class UpdateCompanyReviewCommandValidator : AbstractValidator<UpdateCompanyReviewCommand>
{
    public UpdateCompanyReviewCommandValidator()
    {
        RuleFor(v => v.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");

        RuleFor(v => v.Sentiment)
            .IsInEnum().WithMessage("Please choose Positive or Negative.");

        RuleFor(v => v.Comment)
            .Must(comment => comment is null || comment.Trim().Length <= CompanyReviewRules.MaxCommentLength)
            .WithMessage($"Feedback must not exceed {CompanyReviewRules.MaxCommentLength} characters.");

        RuleFor(v => v.Ratings!)
            .SetValidator(new CompanyReviewRatingsDtoValidator())
            .When(v => v.Ratings is not null);
    }
}

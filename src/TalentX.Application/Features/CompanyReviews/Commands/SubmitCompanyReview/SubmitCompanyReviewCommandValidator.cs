using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;

public sealed class SubmitCompanyReviewCommandValidator : AbstractValidator<SubmitCompanyReviewCommand>
{
    public SubmitCompanyReviewCommandValidator()
    {
        RuleFor(v => v.JobApplicationId)
            .NotEmpty().WithMessage("Job application ID is required.");

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

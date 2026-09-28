using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;

public sealed class HideCompanyReviewCommandValidator : AbstractValidator<HideCompanyReviewCommand>
{
    public const int MinDetailsLength = 10;
    public const int MaxDetailsLength = 1000;

    public HideCompanyReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required.");

        RuleFor(command => command.Reason)
            .IsInEnum()
            .WithMessage("Invalid hide reason.");

        // Details are user-facing (shown to the talent on the hidden review), so a fixed
        // reason alone is never enough explanation.
        // Stop cascade: the length checks only run once NotEmpty has passed, so they never
        // see a null value. (A .When() here would wrap NotEmpty too and let blanks through.)
        RuleFor(command => command.Details)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please explain why this review is being hidden.")
            .Must(details => details.Trim().Length >= MinDetailsLength)
            .WithMessage($"Please explain the reason in at least {MinDetailsLength} characters.")
            .Must(details => details.Trim().Length <= MaxDetailsLength)
            .WithMessage($"Details must be {MaxDetailsLength} characters or fewer.");
    }
}

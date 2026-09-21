using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;

public sealed class ResubmitJobForReviewCommandValidator : AbstractValidator<ResubmitJobForReviewCommand>
{
    public ResubmitJobForReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

using FluentValidation;

namespace TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;

public sealed class RemoveSavedJobCommandValidator : AbstractValidator<RemoveSavedJobCommand>
{
    public RemoveSavedJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID must not be empty.");
    }
}

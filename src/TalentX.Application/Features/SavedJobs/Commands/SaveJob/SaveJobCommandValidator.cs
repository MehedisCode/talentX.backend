using FluentValidation;

namespace TalentX.Application.Features.SavedJobs.Commands.SaveJob;

public sealed class SaveJobCommandValidator : AbstractValidator<SaveJobCommand>
{
    public SaveJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID must not be empty.");
    }
}

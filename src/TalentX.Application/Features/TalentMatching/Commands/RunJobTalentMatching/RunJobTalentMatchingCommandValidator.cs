using FluentValidation;

namespace TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;

public sealed class RunJobTalentMatchingCommandValidator : AbstractValidator<RunJobTalentMatchingCommand>
{
    public RunJobTalentMatchingCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

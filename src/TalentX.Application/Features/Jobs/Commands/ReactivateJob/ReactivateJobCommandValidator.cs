using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.ReactivateJob;

public sealed class ReactivateJobCommandValidator : AbstractValidator<ReactivateJobCommand>
{
    public ReactivateJobCommandValidator()
    {
        RuleFor(command => command.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

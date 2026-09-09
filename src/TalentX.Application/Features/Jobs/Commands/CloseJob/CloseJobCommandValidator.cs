using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.CloseJob;

public sealed class CloseJobCommandValidator : AbstractValidator<CloseJobCommand>
{
    public CloseJobCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

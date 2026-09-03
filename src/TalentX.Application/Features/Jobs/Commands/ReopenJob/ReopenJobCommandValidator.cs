using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.ReopenJob;

public sealed class ReopenJobCommandValidator : AbstractValidator<ReopenJobCommand>
{
    public ReopenJobCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

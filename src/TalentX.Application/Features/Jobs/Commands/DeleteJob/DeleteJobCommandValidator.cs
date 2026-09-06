using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.DeleteJob;

public sealed class DeleteJobCommandValidator : AbstractValidator<DeleteJobCommand>
{
    public DeleteJobCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}

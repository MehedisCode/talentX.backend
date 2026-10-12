using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.DeleteProject;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Project ID is required.");
    }
}

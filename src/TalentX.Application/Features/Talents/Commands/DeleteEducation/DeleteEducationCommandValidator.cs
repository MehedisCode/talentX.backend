using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.DeleteEducation;

public sealed class DeleteEducationCommandValidator : AbstractValidator<DeleteEducationCommand>
{
    public DeleteEducationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

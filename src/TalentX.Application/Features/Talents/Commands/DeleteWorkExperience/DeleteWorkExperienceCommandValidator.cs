using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;

public sealed class DeleteWorkExperienceCommandValidator : AbstractValidator<DeleteWorkExperienceCommand>
{
    public DeleteWorkExperienceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

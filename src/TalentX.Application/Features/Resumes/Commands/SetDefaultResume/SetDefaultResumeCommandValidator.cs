using FluentValidation;

namespace TalentX.Application.Features.Resumes.Commands.SetDefaultResume;

public sealed class SetDefaultResumeCommandValidator : AbstractValidator<SetDefaultResumeCommand>
{
    public SetDefaultResumeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

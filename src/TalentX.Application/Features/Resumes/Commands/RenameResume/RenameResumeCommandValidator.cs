using FluentValidation;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.Commands.RenameResume;

public sealed class RenameResumeCommandValidator : AbstractValidator<RenameResumeCommand>
{
    public RenameResumeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Resume name is required.")
            .MaximumLength(Resume.MaxDisplayNameLength)
            .WithMessage($"Resume name cannot exceed {Resume.MaxDisplayNameLength} characters.");
    }
}

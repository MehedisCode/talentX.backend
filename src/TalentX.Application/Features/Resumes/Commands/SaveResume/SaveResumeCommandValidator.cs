using FluentValidation;
using TalentX.Application.Common.Validation;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.Commands.SaveResume;

public sealed class SaveResumeCommandValidator : AbstractValidator<SaveResumeCommand>
{
    public SaveResumeCommandValidator()
    {
        this.ApplyResumeFileValidationRules(x => x.ResumeFile);

        RuleFor(x => x.DisplayName)
            .MaximumLength(Resume.MaxDisplayNameLength)
            .WithMessage($"Resume name cannot exceed {Resume.MaxDisplayNameLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    }
}

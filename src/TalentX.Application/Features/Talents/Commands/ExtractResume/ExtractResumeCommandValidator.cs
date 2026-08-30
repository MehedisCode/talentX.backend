using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.Talents.Commands.ExtractResume;

public sealed class ExtractResumeCommandValidator : AbstractValidator<ExtractResumeCommand>
{
    public ExtractResumeCommandValidator()
    {
        this.ApplyResumeFileValidationRules(x => x.File);
    }
}

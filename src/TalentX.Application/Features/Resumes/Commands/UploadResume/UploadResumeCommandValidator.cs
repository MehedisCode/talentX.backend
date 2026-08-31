using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.Resumes.Commands.UploadResume;

public sealed class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    public UploadResumeCommandValidator()
    {
        this.ApplyResumeFileValidationRules(x => x.File);
    }
}

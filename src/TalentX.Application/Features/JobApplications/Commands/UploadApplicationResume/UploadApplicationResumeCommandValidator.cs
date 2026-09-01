using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.JobApplications.Commands.UploadApplicationResume;

public sealed class UploadApplicationResumeCommandValidator : AbstractValidator<UploadApplicationResumeCommand>
{
    public UploadApplicationResumeCommandValidator()
    {
        this.ApplyResumeFileValidationRules(x => x.File);
    }
}

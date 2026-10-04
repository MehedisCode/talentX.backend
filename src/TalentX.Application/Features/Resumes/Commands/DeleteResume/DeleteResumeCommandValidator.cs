using FluentValidation;

namespace TalentX.Application.Features.Resumes.Commands.DeleteResume;

public sealed class DeleteResumeCommandValidator : AbstractValidator<DeleteResumeCommand>
{
    public DeleteResumeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

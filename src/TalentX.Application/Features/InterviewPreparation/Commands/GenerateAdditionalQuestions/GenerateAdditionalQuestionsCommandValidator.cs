using FluentValidation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public sealed class GenerateAdditionalQuestionsCommandValidator : AbstractValidator<GenerateAdditionalQuestionsCommand>
{
    public GenerateAdditionalQuestionsCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
    }
}

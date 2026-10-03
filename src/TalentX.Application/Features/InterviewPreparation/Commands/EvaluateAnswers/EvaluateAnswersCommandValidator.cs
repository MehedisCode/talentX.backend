using FluentValidation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;

public sealed class EvaluateAnswersCommandValidator : AbstractValidator<EvaluateAnswersCommand>
{
    public EvaluateAnswersCommandValidator()
    {
        RuleFor(v => v.InvitationId).NotEmpty();
    }
}

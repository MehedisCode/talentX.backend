using FluentValidation;
using TalentX.Application.Features.InterviewPreparation.Constants;

namespace TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;

public sealed class SaveAnswerCommandValidator : AbstractValidator<SaveAnswerCommand>
{
    public SaveAnswerCommandValidator()
    {
        RuleFor(v => v.InvitationId).NotEmpty();
        RuleFor(v => v.Category)
            .NotEmpty()
            .MaximumLength(50)
            .Must(c => InterviewPreparationCategories.All.Contains(c))
            .WithMessage("Category must be a known question category.");
        RuleFor(v => v.QuestionIndex)
            .InclusiveBetween(0, 10_000);
        RuleFor(v => v.Answer)
            .MaximumLength(4000)
            .When(v => v.Answer is not null);
    }
}

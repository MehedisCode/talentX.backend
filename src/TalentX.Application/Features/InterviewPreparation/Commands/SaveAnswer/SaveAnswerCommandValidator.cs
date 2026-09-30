using FluentValidation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;

public sealed class SaveAnswerCommandValidator : AbstractValidator<SaveAnswerCommand>
{
    private static readonly string[] AllowedCategories =
    [
        "Technical", "Behavioral", "Experience-Based", "Role-Specific", "Additional"
    ];

    public SaveAnswerCommandValidator()
    {
        RuleFor(v => v.InvitationId).NotEmpty();
        RuleFor(v => v.Category)
            .NotEmpty()
            .MaximumLength(50)
            .Must(c => AllowedCategories.Contains(c))
            .WithMessage("Category must be a known question category.");
        RuleFor(v => v.QuestionIndex)
            .InclusiveBetween(0, 10_000);
        RuleFor(v => v.Question)
            .NotEmpty()
            .MaximumLength(2000);
        RuleFor(v => v.Answer)
            .MaximumLength(4000)
            .When(v => v.Answer is not null);
    }
}

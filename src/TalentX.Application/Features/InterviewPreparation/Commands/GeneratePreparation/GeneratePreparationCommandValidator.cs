using FluentValidation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;

public sealed class GeneratePreparationCommandValidator : AbstractValidator<GeneratePreparationCommand>
{
    public GeneratePreparationCommandValidator()
    {
        RuleFor(v => v.InvitationId).NotEmpty();
    }
}

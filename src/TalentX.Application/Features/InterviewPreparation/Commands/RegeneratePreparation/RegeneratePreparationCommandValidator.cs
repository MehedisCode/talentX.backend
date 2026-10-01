using FluentValidation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;

public sealed class RegeneratePreparationCommandValidator : AbstractValidator<RegeneratePreparationCommand>
{
    public RegeneratePreparationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("InvitationId is required.");
    }
}

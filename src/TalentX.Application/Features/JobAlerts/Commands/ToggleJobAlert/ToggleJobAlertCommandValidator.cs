using FluentValidation;

namespace TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;

public sealed class ToggleJobAlertCommandValidator : AbstractValidator<ToggleJobAlertCommand>
{
    public ToggleJobAlertCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Job alert ID is required.");
    }
}

using FluentValidation;

namespace TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;

public sealed class DeleteJobAlertCommandValidator : AbstractValidator<DeleteJobAlertCommand>
{
    public DeleteJobAlertCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Job alert ID is required.");
    }
}

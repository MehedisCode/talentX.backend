using FluentValidation;

namespace TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;

public sealed class UpdateApplicationStatusCommandValidator : AbstractValidator<UpdateApplicationStatusCommand>
{
    public UpdateApplicationStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus)
            .Must(JobApplicationRules.IsManuallyAssignable)
            .WithMessage("Status '{PropertyValue}' cannot be set manually.");
    }
}

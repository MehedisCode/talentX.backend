using FluentValidation;

namespace TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;

public sealed class UpdateJobAlertCommandValidator : AbstractValidator<UpdateJobAlertCommand>
{
    public UpdateJobAlertCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Job alert ID is required.");

        RuleFor(x => x.Parameters).NotNull();

        RuleFor(x => x.Parameters.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Parameters.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Parameters.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

        RuleFor(x => x.Parameters.EmploymentType)
            .IsInEnum().When(x => x.Parameters.EmploymentType.HasValue);

        RuleFor(x => x.Parameters.ExperienceLevel)
            .IsInEnum().When(x => x.Parameters.ExperienceLevel.HasValue);

        RuleFor(x => x.Parameters.WorkArrangement)
            .IsInEnum().When(x => x.Parameters.WorkArrangement.HasValue);
    }
}

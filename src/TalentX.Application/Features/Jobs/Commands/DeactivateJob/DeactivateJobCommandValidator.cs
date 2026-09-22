using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.DeactivateJob;

public sealed class DeactivateJobCommandValidator : AbstractValidator<DeactivateJobCommand>
{
    public const int MinDetailsLength = 10;
    public const int MaxDetailsLength = 1000;

    public DeactivateJobCommandValidator()
    {
        RuleFor(command => command.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");

        RuleFor(command => command.Reason)
            .IsInEnum()
            .WithMessage("Invalid deactivation reason.");

        // Details are mandatory even with a fixed reason: "Other" or "Misleading information"
        // alone doesn't tell the employer what to fix before resubmitting.
        // Stop cascade: the length checks only run once NotEmpty has passed, so they never
        // see a null value. (A .When() here would wrap NotEmpty too and let blanks through.)
        RuleFor(command => command.Details)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please explain why this job is being deactivated.")
            .Must(details => details.Trim().Length >= MinDetailsLength)
            .WithMessage($"Please explain the reason in at least {MinDetailsLength} characters.")
            .Must(details => details.Trim().Length <= MaxDetailsLength)
            .WithMessage($"Details must be {MaxDetailsLength} characters or fewer.");
    }
}

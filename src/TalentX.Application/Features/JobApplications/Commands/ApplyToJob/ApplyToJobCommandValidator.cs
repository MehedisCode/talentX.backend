using FluentValidation;

namespace TalentX.Application.Features.JobApplications.Commands.ApplyToJob;

// Simple validator for the ApplyToJobCommand using FluentValidation
public sealed class ApplyToJobCommandValidator : AbstractValidator<ApplyToJobCommand>
{
    public ApplyToJobCommandValidator()
    {
        RuleFor(x => x.ApplicantName)
            .NotEmpty().WithMessage("Applicant name is required.")
            .MaximumLength(120).WithMessage("Applicant name cannot exceed 120 characters.");

        RuleFor(x => x.ApplicantEmail)
            .NotEmpty().WithMessage("Applicant email is required.")
            .MaximumLength(255).WithMessage("Applicant email cannot exceed 255 characters.")
            .EmailAddress().WithMessage("Applicant email must be a valid email address.");

        RuleFor(x => x.ResumeStorageKey)
            .NotEmpty().WithMessage("A resume is required to apply.")
            .MaximumLength(500).WithMessage("Resume reference cannot exceed 500 characters.")
            .When(x => !x.ResumeId.HasValue);

        RuleFor(x => x.CoverLetter)
            .Must(cl => cl is null || cl.Replace("\r\n", "\n").Replace("\r", "\n").Trim().Length <= 4000)
            .WithMessage("Cover letter cannot exceed 4000 characters.");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location cannot exceed 200 characters.");

        RuleFor(x => x.ExpectedSalary)
            .GreaterThanOrEqualTo(0).WithMessage("Expected salary cannot be negative.")
            .LessThanOrEqualTo(9_999_999_999.99m).WithMessage("Expected salary is too large.")
            .When(x => x.ExpectedSalary.HasValue);

        RuleFor(x => x.TotalYearsExperience)
            .InclusiveBetween(0, 60).WithMessage("Years of experience must be between 0 and 60.")
            .When(x => x.TotalYearsExperience.HasValue);
    }
}

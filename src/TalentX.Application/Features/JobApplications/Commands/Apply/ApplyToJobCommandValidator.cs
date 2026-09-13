using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandValidator : AbstractValidator<ApplyToJobCommand>
{
    public ApplyToJobCommandValidator()
    {
        RuleFor(v => v.JobId)
            .NotEmpty().WithMessage("Job is required.");

        RuleFor(v => v.ApplicantName)
            .NotNull().WithMessage("Name is required.")
            .Must(name => name is null || name.Trim().Length >= 2)
                .WithMessage("Name must be at least 2 characters.")
            .MaximumLength(120).WithMessage("Name must not exceed 120 characters.")
            .Must(name => name is null || name.Trim().Length == 0 || name.Trim().Any(char.IsLetter))
                .WithMessage("Name must contain at least one letter.");

        RuleFor(v => v.ApplicantEmail)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        this.ApplyResumeFileValidationRules(v => v.ResumeFile);

        RuleFor(v => v.CoverLetter)
            .NotNull().WithMessage("Cover letter is required.")
            .Must(coverLetter => coverLetter is null || NormalizeLine(coverLetter).Trim().Length >= 2)
                .WithMessage("Cover letter must be at least 2 characters.")
            .Must(coverLetter => coverLetter is null || NormalizeLine(coverLetter).Trim().Length <= 4000)
                .WithMessage("Cover letter must not exceed 4000 characters.")
            .Must(coverLetter => coverLetter is null || NormalizeLine(coverLetter).Trim().Length == 0 || NormalizeLine(coverLetter).Trim().Any(char.IsLetter))
                .WithMessage("Cover letter must contain at least one letter.");

        RuleFor(v => v.ExpectedSalary)
            .GreaterThan(0).WithMessage("Expected salary must be greater than zero.");

        RuleFor(v => v.TotalYearsExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience cannot be negative.")
            .LessThanOrEqualTo(80).WithMessage("Years of experience must not exceed 80.");

        RuleFor(v => v.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");
    }

    private static string NormalizeLine(string value) =>
        value.Replace("\r\n", "\n").Replace("\r", "\n");
}
// using System.Linq;
using FluentValidation;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        // Content strictness (min-length, must-contain-a-letter, etc.) only applies when
        // actually publishing. A draft save (Publish == false) can be saved from any wizard
        // step with these fields still blank — JobRules.EnsurePublishable is the real gate
        // that blocks an incomplete job from ever going live.
        // Title is always required — even for drafts — so the job has a name.
        RuleFor(v => v.Title)
            .NotNull().WithMessage("Title is required.")
            .NotEmpty().WithMessage("Title is required.");

        // Strict title rules only apply when publishing.
        RuleFor(v => v.Title)
            .Must(title => title is null || title.Trim().Length >= 2)
                .WithMessage("Title must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .Must(title => title is null || title.Trim().Length == 0 || !char.IsDigit(title.Trim()[0]))
                .WithMessage("Title cannot start with a number.")
            .Must(title => title is null || title.Trim().Length == 0 || title.Trim().Any(char.IsLetter))
                .WithMessage("Title must contain at least one letter.")
            .When(v => v.Publish);

        RuleFor(v => v.CompanyName)
            .NotNull().WithMessage("Company name is required.")
            .Must(companyName => companyName is null || companyName.Trim().Length >= 2)
                .WithMessage("Company name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .Must(companyName => companyName is null || companyName.Trim().Length == 0 || companyName.Trim().Any(char.IsLetter))
                .WithMessage("Company name must contain at least one letter.")
            .When(v => v.Publish);

        RuleFor(v => v.Description)
            .NotNull().WithMessage("Description is required.")
            .Must(description => description is null || description.Trim().Length >= 20)
                .WithMessage("Description must be at least 20 characters.")
            .MaximumLength(10000).WithMessage("Description must not exceed 10000 characters.")
            .Must(description => description is null || description.Trim().Length == 0 || description.Trim().Any(char.IsLetter))
                .WithMessage("Description must contain at least one letter.")
            .When(v => v.Publish);

        RuleFor(v => v.Location)
            .NotNull().WithMessage("Location is required.")
            .Must(location => location is null || location.Trim().Length >= 2)
                .WithMessage("Location must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
            .Must(location => location is null || location.Trim().Length == 0 || location.Trim().Any(char.IsLetter))
                .WithMessage("Location cannot be purely numeric.")
            .When(v => v.Publish);

        RuleFor(v => v.EmploymentType)
            .IsInEnum().WithMessage("Invalid EmploymentType.");

        RuleFor(v => v.ExperienceLevel)
            .IsInEnum().WithMessage("Invalid ExperienceLevel.");

        RuleFor(v => v.WorkArrangement)
            .IsInEnum().WithMessage("Invalid WorkArrangement.");

        RuleFor(v => v.Vacancies)
            .GreaterThanOrEqualTo(0).WithMessage("Vacancies cannot be negative.");

        RuleForEach(v => v.RequiredSkills)
            .NotEmpty().WithMessage("Skill entries cannot be blank.")
            .Must(skill => string.IsNullOrWhiteSpace(skill) || skill.Trim().Any(char.IsLetter))
                .WithMessage("Skill entries must contain at least one letter.");

        RuleFor(v => v.ExpireAt)
            .Must(expireAt => expireAt.Date >= DateTime.UtcNow.Date)
            .WithMessage("Expire date must be today or in the future.")
            .When(v => v.Publish);

        // Conditional validation for wizard fields — only enforced when publishing.
        // Drafts can be saved from any step with these fields still blank.
        RuleFor(v => v.City)
            .Must((cmd, city) => !IsLocationRequired(cmd.WorkArrangement)
                || (!string.IsNullOrWhiteSpace(city) && city.Trim().Length >= 2))
            .WithMessage("City is required for Hybrid and On-Site roles.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(v => v.Publish && v.WorkArrangement is WorkArrangement.OnSite or WorkArrangement.Hybrid);

        RuleFor(v => v.Country)
            .Must((cmd, country) => !IsLocationRequired(cmd.WorkArrangement)
                || (!string.IsNullOrWhiteSpace(country) && country.Trim().Length >= 2))
            .WithMessage("Country is required for Hybrid and On-Site roles.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(v => v.Publish && v.WorkArrangement is WorkArrangement.OnSite or WorkArrangement.Hybrid);

        RuleFor(v => v.MinOverlapHours)
            .Must((cmd, hours) => !IsRemoteTimezoneRequired(cmd.WorkArrangement)
                || (hours.HasValue && hours.Value > 0))
            .WithMessage("Minimum overlap hours is required for Remote roles.")
            .When(v => v.Publish && v.WorkArrangement is WorkArrangement.FullRemote);

        // Salary is optional in a draft (SalaryType can be left unset entirely)
        // but mandatory to publish — JobRules.EnsurePublishable enforces the
        // "must be set" half; these rules enforce "must be internally
        // consistent" (always) and "must be complete for its type" (publish
        // only), mirroring how Title/CompanyName content-strictness is
        // publish-gated while their base NotNull checks are not.
        RuleFor(v => v.SalaryType)
            .IsInEnum().WithMessage("Invalid SalaryType.")
            .When(v => v.SalaryType.HasValue);

        RuleFor(v => v.SalaryCurrency)
            .IsInEnum().WithMessage("Invalid SalaryCurrency.")
            .When(v => v.SalaryCurrency.HasValue);

        RuleFor(v => v.SalaryPeriod)
            .IsInEnum().WithMessage("Invalid SalaryPeriod.")
            .When(v => v.SalaryPeriod.HasValue);

        RuleFor(v => v.SalaryMax)
            .GreaterThanOrEqualTo(v => v.SalaryMin!.Value)
                .WithMessage("Maximum salary must be greater than or equal to minimum salary.")
            .When(v => v.SalaryMin.HasValue && v.SalaryMax.HasValue);

        RuleFor(v => v.SalaryType)
            .NotNull().WithMessage("Salary is required.")
            .When(v => v.Publish);

        RuleFor(v => v.SalaryCurrency)
            .NotNull().WithMessage("Salary currency is required.")
            .When(v => v.Publish && v.SalaryType is SalaryType.Fixed or SalaryType.Range);

        RuleFor(v => v.SalaryPeriod)
            .NotNull().WithMessage("Salary period is required.")
            .When(v => v.Publish && v.SalaryType is SalaryType.Fixed or SalaryType.Range);

        RuleFor(v => v.SalaryMin)
            .NotNull().WithMessage("Salary amount is required.")
            .When(v => v.Publish && v.SalaryType is SalaryType.Fixed or SalaryType.Range);

        RuleFor(v => v.SalaryMax)
            .NotNull().WithMessage("Maximum salary is required.")
            .When(v => v.Publish && v.SalaryType == SalaryType.Range);

        // Requirements — optional in a draft, mandatory to publish. Same
        // content-strictness-is-publish-gated pattern as Description.
        RuleFor(v => v.Requirements)
            .MaximumLength(5000).WithMessage("Requirements must not exceed 5000 characters.");

        RuleFor(v => v.Requirements)
            .Must(requirements => !string.IsNullOrWhiteSpace(requirements))
                .WithMessage("Requirements is required.")
            .When(v => v.Publish);
    }

    private static bool IsLocationRequired(WorkArrangement a) =>
        a is WorkArrangement.OnSite or WorkArrangement.Hybrid;

    private static bool IsRemoteTimezoneRequired(WorkArrangement a) =>
        a is WorkArrangement.FullRemote;
}

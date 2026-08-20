using System.Linq;
using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        // A null field means "don't change this field" — every rule below is a
        // no-op when the value is null, so only fields actually sent get checked.
        // Completeness ("is this ready to go live") is enforced solely by
        // Job.EnsureIsPublishable() in the domain, triggered when Publish: true.

        // Every content check below runs against value.Trim(), not the raw
        // value, so an update that sets a field to whitespace-only (e.g. five
        // spaces) can't slip past by counting padding as content. Untouched
        // (null) fields still pass straight through, preserving the
        // "null = don't change" contract.
        RuleFor(v => v.Title)
            .Must(title => title is null || title.Trim().Length >= 2)
                .WithMessage("Title must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .Must(title => title is null || title.Trim().Length == 0 || !char.IsDigit(title.Trim()[0]))
                .WithMessage("Title cannot start with a number.")
            .Must(title => title is null || title.Trim().Length == 0 || title.Trim().Any(char.IsLetter))
                .WithMessage("Title must contain at least one letter.");

        RuleFor(v => v.CompanyName)
            .Must(companyName => companyName is null || companyName.Trim().Length >= 2)
                .WithMessage("Company name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .Must(companyName => companyName is null || companyName.Trim().Length == 0 || companyName.Trim().Any(char.IsLetter))
                .WithMessage("Company name must contain at least one letter.");

        RuleFor(v => v.Description)
            .Must(description => description is null || description.Trim().Length >= 20)
                .WithMessage("Description must be at least 20 characters.")
            .MaximumLength(10000).WithMessage("Description must not exceed 10000 characters.")
            .Must(description => description is null || description.Trim().Length == 0 || description.Trim().Any(char.IsLetter))
                .WithMessage("Description must contain at least one letter.");

        RuleFor(v => v.Location)
            .Must(location => location is null || location.Trim().Length >= 2)
                .WithMessage("Location must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
            .Must(location => location is null || location.Trim().Length == 0 || location.Trim().Any(char.IsLetter))
                .WithMessage("Location cannot be purely numeric.");

        RuleFor(v => v.EmploymentType)
            .IsInEnum().WithMessage("Invalid EmploymentType.");

        RuleFor(v => v.ExperienceLevel)
            .IsInEnum().WithMessage("Invalid ExperienceLevel.");

        RuleFor(v => v.WorkArrangement)
            .IsInEnum().WithMessage("Invalid WorkArrangement.");

        RuleFor(v => v.Vacancies)
            .GreaterThanOrEqualTo(0).WithMessage("Vacancies cannot be negative.");

        RuleForEach(v => v.RequiredSkills)
            .NotEmpty().WithMessage("Skill entries cannot be blank.");

        RuleFor(v => v.ExpireAt)
            .GreaterThan(v => DateTime.UtcNow).WithMessage("Expire date must be in the future.");
    }
}

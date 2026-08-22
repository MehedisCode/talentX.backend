using System.Linq;
using FluentValidation;

namespace TalentX.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(v => v.Title)
            .Must(title => title is null || title.Trim().Length >= 5)
                .WithMessage("Title must be at least 5 characters.")
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
            .Must(location => location is null || location.Trim().Length >= 5)
                .WithMessage("Location must be at least 5 characters.")
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
            .NotEmpty().WithMessage("Skill entries cannot be blank.")
            .Must(skill => string.IsNullOrWhiteSpace(skill) || skill.Trim().Any(char.IsLetter))
                .WithMessage("Skill entries must contain at least one letter.");

        RuleFor(v => v.ExpireAt)
            .GreaterThan(v => DateTime.UtcNow).WithMessage("Expire date must be in the future.");
    }
}

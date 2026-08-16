using FluentValidation;

namespace TalentX.Application.Jobs.Commands.CreateJob;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        // Structural/type-safety rules only — always enforced, draft or publish.
        // Completeness ("is this ready to go live") is enforced solely by
        // Job.EnsureIsPublishable() in the domain, triggered when Publish: true.

        RuleFor(v => v.Title)
            .NotNull().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(v => v.Description)
            .NotNull().WithMessage("Description is required.")
            .MaximumLength(10000).WithMessage("Description must not exceed 10000 characters.");

        RuleFor(v => v.Location)
            .NotNull().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

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
    }
}

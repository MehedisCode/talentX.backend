using FluentValidation;

namespace TalentX.Application.Jobs.Commands.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        // A null field means "don't change this field" — every rule below is a
        // no-op when the value is null, so only fields actually sent get checked.
        // Completeness ("is this ready to go live") is enforced solely by
        // Job.EnsureIsPublishable() in the domain, triggered when Publish: true.

        RuleFor(v => v.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(10000).WithMessage("Description must not exceed 10000 characters.");

        RuleFor(v => v.Location)
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

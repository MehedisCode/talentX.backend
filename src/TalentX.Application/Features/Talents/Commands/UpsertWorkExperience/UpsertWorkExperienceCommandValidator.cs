using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;

public sealed class UpsertWorkExperienceCommandValidator : AbstractValidator<UpsertWorkExperienceCommand>
{
    public UpsertWorkExperienceCommandValidator()
    {
        RuleFor(command => command.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(command => command.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.");

        RuleFor(command => command.StartDate)
            .MaximumLength(50).WithMessage("Start date must not exceed 50 characters.");

        RuleFor(command => command.EndDate)
            .MaximumLength(50).WithMessage("End date must not exceed 50 characters.");
    }
}

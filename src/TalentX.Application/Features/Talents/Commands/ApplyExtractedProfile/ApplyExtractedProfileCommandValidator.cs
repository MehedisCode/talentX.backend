using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed class ApplyExtractedProfileCommandValidator : AbstractValidator<ApplyExtractedProfileCommand>
{
    public ApplyExtractedProfileCommandValidator()
    {
        RuleFor(command => command.TechnicalSkills).NotNull();
        RuleFor(command => command.SoftSkills).NotNull();
        RuleFor(command => command.ToolsAndPlatforms).NotNull();
        RuleFor(command => command.WorkExperiences).NotNull();
        RuleFor(command => command.Educations).NotNull();

        this.ApplyResumeFileValidationRules(command => command.ResumeFile);

        RuleForEach(command => command.WorkExperiences).ChildRules(workExperience =>
        {
            workExperience.RuleFor(item => item.CompanyName)
                .NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

            workExperience.RuleFor(item => item.JobTitle)
                .NotEmpty().WithMessage("Job title is required.")
                .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.");

            workExperience.RuleFor(item => item.StartDate)
                .MaximumLength(50).WithMessage("Start date must not exceed 50 characters.");

            workExperience.RuleFor(item => item.EndDate)
                .MaximumLength(50).WithMessage("End date must not exceed 50 characters.");
        });

        RuleForEach(command => command.Educations).ChildRules(education =>
        {
            education.RuleFor(item => item.Institution)
                .NotEmpty().WithMessage("Institution is required.")
                .MaximumLength(200).WithMessage("Institution must not exceed 200 characters.");

            education.RuleFor(item => item.Degree)
                .NotEmpty().WithMessage("Degree is required.")
                .MaximumLength(200).WithMessage("Degree must not exceed 200 characters.");

            education.RuleFor(item => item.StartDate)
                .MaximumLength(50).WithMessage("Start date must not exceed 50 characters.");

            education.RuleFor(item => item.EndDate)
                .MaximumLength(50).WithMessage("End date must not exceed 50 characters.");
        });
    }
}

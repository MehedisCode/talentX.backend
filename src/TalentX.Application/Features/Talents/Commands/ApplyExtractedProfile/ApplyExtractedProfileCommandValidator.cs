using FluentValidation;
using TalentX.Application.Common.Validation;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed class ApplyExtractedProfileCommandValidator : AbstractValidator<ApplyExtractedProfileCommand>
{
    public ApplyExtractedProfileCommandValidator()
    {
        RuleFor(command => command.ProfileData)
            .NotNull().WithMessage("Profile data is required.");

        When(command => command.ProfileData is not null, () =>
        {
            RuleFor(command => command.ProfileData.FullName)
                .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.")
                .When(command => command.ProfileData.FullName is not null);

            RuleFor(command => command.ProfileData.Tagline)
                .MaximumLength(160).WithMessage("Tagline must not exceed 160 characters.")
                .When(command => command.ProfileData.Tagline is not null);

            RuleFor(command => command.ProfileData.ShortBio)
                .MaximumLength(1000).WithMessage("Short bio must not exceed 1000 characters.")
                .When(command => command.ProfileData.ShortBio is not null);

            RuleFor(command => command.ProfileData.Location)
                .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
                .When(command => command.ProfileData.Location is not null);

            RuleFor(command => command.ProfileData.Phone)
                .MaximumLength(40).WithMessage("Phone must not exceed 40 characters.")
                .When(command => command.ProfileData.Phone is not null);

            RuleFor(command => command.ProfileData.LinkedInUrl)
                .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters.")
                .Must(BeAValidAbsoluteUrl).WithMessage("LinkedIn URL must be a valid URL.")
                .When(command => !string.IsNullOrWhiteSpace(command.ProfileData.LinkedInUrl));

            RuleFor(command => command.ProfileData.ExpectedSalary)
                .GreaterThanOrEqualTo(0).WithMessage("Expected salary cannot be negative.")
                .When(command => command.ProfileData.ExpectedSalary.HasValue);

            RuleFor(command => command.ProfileData.TotalYearsExperience)
                .GreaterThanOrEqualTo(0).WithMessage("Total years of experience cannot be negative.")
                .When(command => command.ProfileData.TotalYearsExperience.HasValue);

            RuleForEach(command => command.ProfileData.AdditionalUrls)
                .Must(BeAValidAbsoluteUrl).WithMessage("Additional URLs must be valid URLs.");

            RuleForEach(command => command.ProfileData.WorkExperiences).ChildRules(workExperience =>
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

            RuleForEach(command => command.ProfileData.Educations).ChildRules(education =>
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
        });

        this.ApplyResumeFileValidationRules(command => command.ResumeFile);
    }

    private static bool BeAValidAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out _);
}

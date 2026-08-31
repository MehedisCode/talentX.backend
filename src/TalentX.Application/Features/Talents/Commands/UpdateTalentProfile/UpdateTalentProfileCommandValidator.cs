using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;

public sealed class UpdateTalentProfileCommandValidator : AbstractValidator<UpdateTalentProfileCommand>
{
    public UpdateTalentProfileCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(120).WithMessage("Name must not exceed 120 characters.");

        RuleFor(command => command.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

        RuleFor(command => command.Tagline)
            .MaximumLength(160).WithMessage("Tagline must not exceed 160 characters.");

        RuleFor(command => command.ShortBio)
            .MaximumLength(1000).WithMessage("Short bio must not exceed 1000 characters.");

        RuleFor(command => command.Phone)
            .MaximumLength(40).WithMessage("Phone must not exceed 40 characters.");

        RuleFor(command => command.LinkedInUrl)
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters.")
            .Must(BeAValidAbsoluteUrl).WithMessage("LinkedIn URL must be a valid URL.")
            .When(command => !string.IsNullOrWhiteSpace(command.LinkedInUrl));

        RuleFor(command => command.ExpectedSalary)
            .GreaterThanOrEqualTo(0).WithMessage("Expected salary cannot be negative.");

        RuleFor(command => command.TotalYearsExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Total years of experience cannot be negative.");

        RuleForEach(command => command.AdditionalUrls)
            .Must(BeAValidAbsoluteUrl).WithMessage("Additional URLs must be valid URLs.");
    }

    private static bool BeAValidAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out _);
}

using FluentValidation;
using TalentX.Application.Features.Talents.Constants;

namespace TalentX.Application.Features.Talents.Commands.UpsertProject;

public sealed class UpsertProjectCommandValidator : AbstractValidator<UpsertProjectCommand>
{
    public UpsertProjectCommandValidator()
    {
        RuleFor(command => command.ProjectName)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(ProjectConstants.MaxProjectNameLength)
            .WithMessage($"Project name must not exceed {ProjectConstants.MaxProjectNameLength} characters.");

        RuleFor(command => command.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(ProjectConstants.MaxRoleLength)
            .WithMessage($"Role must not exceed {ProjectConstants.MaxRoleLength} characters.");

        RuleFor(command => command.ProjectType)
            .NotEmpty().WithMessage("Project type is required.")
            .MaximumLength(ProjectConstants.MaxProjectTypeLength)
            .WithMessage($"Project type must not exceed {ProjectConstants.MaxProjectTypeLength} characters.")
            .Must(type => ProjectConstants.AllowedProjectTypes.Any(allowed => string.Equals(allowed, type, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"Project type must be one of: {string.Join(", ", ProjectConstants.AllowedProjectTypes)}.");

        RuleFor(command => command.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .MaximumLength(ProjectConstants.MaxStartDateLength)
            .WithMessage($"Start date must not exceed {ProjectConstants.MaxStartDateLength} characters.");

        RuleFor(command => command.EndDate)
            .MaximumLength(ProjectConstants.MaxEndDateLength)
            .WithMessage($"End date must not exceed {ProjectConstants.MaxEndDateLength} characters.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(ProjectConstants.MaxDescriptionLength)
            .WithMessage($"Description must not exceed {ProjectConstants.MaxDescriptionLength} characters.");

        RuleFor(command => command.TechnologiesUsed)
            .Must(technologies => technologies == null || technologies.Count <= ProjectConstants.MaxTechnologiesCount)
            .WithMessage($"You can specify up to {ProjectConstants.MaxTechnologiesCount} technologies.");

        RuleForEach(command => command.TechnologiesUsed)
            .MaximumLength(ProjectConstants.MaxTechnologyTagLength)
            .WithMessage($"Technology tag must not exceed {ProjectConstants.MaxTechnologyTagLength} characters.");

        RuleFor(command => command.ProjectUrl)
            .MaximumLength(ProjectConstants.MaxProjectUrlLength)
            .WithMessage($"Project URL must not exceed {ProjectConstants.MaxProjectUrlLength} characters.")
            .Must(BeAValidUrl)
            .WithMessage("Project URL must be a valid HTTP or HTTPS URL.")
            .When(command => !string.IsNullOrWhiteSpace(command.ProjectUrl));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

using System;
using FluentValidation;

namespace TalentX.Application.Features.Companies.Commands.UpdateCompanyProfile;

public sealed class UpdateCompanyProfileCommandValidator : AbstractValidator<UpdateCompanyProfileCommand>
{
    public UpdateCompanyProfileCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is mandatory.")
            .MinimumLength(2).WithMessage("Company name must be at least 2 characters.");

        RuleFor(x => x.Website)
            .Must(BeAValidUrl).WithMessage("Website must be a valid URL.");

        RuleFor(x => x.LinkedIn)
            .Must(BeAValidUrl).WithMessage("LinkedIn must be a valid URL.");

        RuleFor(x => x.Twitter)
            .Must(BeAValidUrl).WithMessage("Twitter must be a valid URL.");

        RuleFor(x => x.Facebook)
            .Must(BeAValidUrl).WithMessage("Facebook must be a valid URL.");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

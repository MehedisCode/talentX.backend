using FluentValidation.TestHelper;
using TalentX.Application.Features.Companies.Commands.UpdateCompanyProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Companies;

public sealed class UpdateCompanyProfileCommandValidatorTests
{
    private readonly UpdateCompanyProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_WhenValidPayload()
    {
        var command = new UpdateCompanyProfileCommand(
            "Tech Innovations",
            "Leading tech startup",
            "Information Technology",
            "51-200",
            "https://techinnovations.com",
            "San Francisco, CA",
            "https://linkedin.com/company/techinnovations",
            "https://twitter.com/techinnovations",
            "https://facebook.com/techinnovations"
        );

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Validate_Fails_WhenCompanyNameIsInvalid(string companyName)
    {
        var command = new UpdateCompanyProfileCommand(
            companyName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Validate_Fails_WhenUrlsAreInvalid()
    {
        var command = new UpdateCompanyProfileCommand(
            "Acme Corp",
            null,
            null,
            null,
            "not-a-url",
            null,
            "invalid-linkedin",
            null,
            null
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Website);
        result.ShouldHaveValidationErrorFor(x => x.LinkedIn);
    }
}

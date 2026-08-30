using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpdateTalentProfile;

public class UpdateTalentProfileCommandValidatorTests
{
    private readonly UpdateTalentProfileCommandValidator _validator = new();

    private static UpdateTalentProfileCommand ValidCommand() => new(
        "Jane Doe",
        "Dhaka",
        "Backend Engineer",
        "Five years of backend experience.",
        "+8801000000000",
        "https://linkedin.com/in/janedoe",
        new List<string> { "https://portfolio.example.com" },
        5m,
        120000m,
        new List<string> { "Scalable systems" }
    );

    [Fact]
    public void Validate_WithAValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingName_HasError(string? name)
    {
        var command = ValidCommand() with { Name = name! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_WithAnInvalidLinkedInUrl_HasError()
    {
        var command = ValidCommand() with { LinkedInUrl = "not-a-url" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LinkedInUrl);
    }

    [Fact]
    public void Validate_WithShortBioOver1000Characters_HasError()
    {
        var command = ValidCommand() with { ShortBio = new string('a', 1001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ShortBio);
    }

    [Fact]
    public void Validate_WithANegativeExpectedSalary_HasError()
    {
        var command = ValidCommand() with { ExpectedSalary = -1m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Fact]
    public void Validate_WithANegativeTotalYearsExperience_HasError()
    {
        var command = ValidCommand() with { TotalYearsExperience = -1m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public void Validate_WithTaglineOver160Characters_HasError()
    {
        var command = ValidCommand() with { Tagline = new string('a', 161) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Tagline);
    }

    [Fact]
    public void Validate_WithLocationOver200Characters_HasError()
    {
        var command = ValidCommand() with { Location = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithAnInvalidAdditionalUrl_HasError()
    {
        var command = ValidCommand() with { AdditionalUrls = new List<string> { "not-a-url" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("AdditionalUrls[0]");
    }
}

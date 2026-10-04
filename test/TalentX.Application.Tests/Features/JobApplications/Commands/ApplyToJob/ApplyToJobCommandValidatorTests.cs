using FluentValidation.TestHelper;
using TalentX.Application.Features.JobApplications.Commands.ApplyToJob;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.ApplyToJob;

public class ApplyToJobCommandValidatorTests
{
    private readonly ApplyToJobCommandValidator _validator = new();

    private static ApplyToJobCommand ValidCommand() => new(
        Guid.NewGuid(),
        "Jane Talent",
        "jane@example.com",
        "resumes/abc_cv.pdf",
        "I am a great fit for this role.",
        90000m,
        5m,
        "Dhaka");

    [Fact]
    public void Validate_WithAValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingApplicantName_HasError(string? name)
    {
        var command = ValidCommand() with { ApplicantName = name! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Fact]
    public void Validate_WithApplicantNameOver120Characters_HasError()
    {
        var command = ValidCommand() with { ApplicantName = new string('a', 121) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidApplicantEmail_HasError(string? email)
    {
        var command = ValidCommand() with { ApplicantEmail = email! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingResumeStorageKey_HasError(string? key)
    {
        var command = ValidCommand() with { ResumeStorageKey = key! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ResumeStorageKey);
    }

    [Fact]
    public void Validate_WithResumeIdAndNoStorageKey_HasNoError()
    {
        var command = ValidCommand() with { ResumeStorageKey = string.Empty, ResumeId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ResumeStorageKey);
    }

    [Fact]
    public void Validate_WithResumeIdAndBlankStorageKey_HasNoError()
    {
        var command = ValidCommand() with { ResumeStorageKey = "   ", ResumeId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ResumeStorageKey);
    }

    [Fact]
    public void Validate_WithResumeStorageKeyOver500Characters_HasError()
    {
        var command = ValidCommand() with { ResumeStorageKey = new string('a', 501) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ResumeStorageKey);
    }

    [Fact]
    public void Validate_WithCoverLetterOver4000Characters_HasError()
    {
        var command = ValidCommand() with { CoverLetter = new string('a', 4001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter);
    }

    [Fact]
    public void Validate_WithLocationOver200Characters_HasError()
    {
        var command = ValidCommand() with { Location = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithNegativeExpectedSalary_HasError()
    {
        var command = ValidCommand() with { ExpectedSalary = -1m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Fact]
    public void Validate_WithExpectedSalaryAboveTheNumericCeiling_HasError()
    {
        var command = ValidCommand() with { ExpectedSalary = 10_000_000_000m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(61)]
    public void Validate_WithYearsOfExperienceOutOfRange_HasError(double years)
    {
        var command = ValidCommand() with { TotalYearsExperience = (decimal)years };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public void Validate_WithNullOptionalNumbers_HasNoError()
    {
        var command = ValidCommand() with
        {
            ExpectedSalary = null,
            TotalYearsExperience = null,
            CoverLetter = null,
            Location = null,
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

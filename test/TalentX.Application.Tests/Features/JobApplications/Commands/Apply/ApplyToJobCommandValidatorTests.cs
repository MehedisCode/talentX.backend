using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandValidatorTests
{
    private readonly ApplyToJobCommandValidator _validator = new();

    private static ApplyToJobCommand ValidCommand() => new(
        Guid.NewGuid(),
        "Jane Doe",
        "jane@example.com",
        "uploads/1234/resume.pdf",
        "I am excited to apply for this role.",
        85000,
        6,
        "Dhaka"
    );

    [Fact]
    public void Validate_WithAValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyJobId_HasError()
    {
        var command = ValidCommand() with { JobId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.JobId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("J")]
    public void Validate_WithInvalidName_HasError(string? name)
    {
        var command = ValidCommand() with { ApplicantName = name! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Fact]
    public void Validate_WithNameOver120Characters_HasError()
    {
        var command = ValidCommand() with { ApplicantName = new string('a', 121) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Fact]
    public void Validate_WithNameContainingNoLetters_HasError()
    {
        var command = ValidCommand() with { ApplicantName = "12345678" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName)
            .WithErrorMessage("Name must contain at least one letter.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingEmail_HasError(string? email)
    {
        var command = ValidCommand() with { ApplicantEmail = email! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("jane@")]
    [InlineData("@example.com")]
    public void Validate_WithInvalidEmail_HasError(string email)
    {
        var command = ValidCommand() with { ApplicantEmail = email };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Validate_WithSuccessfulEmail_HasNoError()
    {
        var command = ValidCommand() with { ApplicantEmail = "jane.doe+work@example.co.uk" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Fact]
    public void Validate_WithEmailOver255Characters_HasError()
    {
        var command = ValidCommand() with { ApplicantEmail = $"jane@{new string('a', 250)}.com" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingResumeStorageKey_HasError(string? resumeKey)
    {
        var command = ValidCommand() with { ResumeStorageKey = resumeKey! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ResumeStorageKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("T")]
    public void Validate_WithInvalidCoverLetter_HasError(string? coverLetter)
    {
        var command = ValidCommand() with { CoverLetter = coverLetter! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter);
    }

    [Fact]
    public void Validate_WithCoverLetterOver4000Characters_HasError()
    {
        var command = ValidCommand() with { CoverLetter = new string('a', 4001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter);
    }

    [Fact]
    public void Validate_WithCoverLetterThatIsAllWhitespace_HasError()
    {
        var command = ValidCommand() with { CoverLetter = new string(' ', 25) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter)
            .WithErrorMessage("Cover letter must be at least 2 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Validate_WithNonPositiveExpectedSalary_HasError(decimal salary)
    {
        var command = ValidCommand() with { ExpectedSalary = salary };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Fact]
    public void Validate_WithPositiveExpectedSalary_HasNoError()
    {
        var command = ValidCommand() with { ExpectedSalary = 50000 };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    public void Validate_WithOutOfRangeExperience_HasError(decimal years)
    {
        var command = ValidCommand() with { TotalYearsExperience = years };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public void Validate_WithZeroExperience_HasNoError()
    {
        var command = ValidCommand() with { TotalYearsExperience = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public void Validate_WithLocationOver200Characters_HasError()
    {
        var command = ValidCommand() with { Location = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithNullLocation_HasNoError()
    {
        var command = ValidCommand() with { Location = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Location);
    }
}
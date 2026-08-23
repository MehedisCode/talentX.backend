using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandValidatorTests
{
    private readonly CreateJobCommandValidator _validator = new();

    private static CreateJobCommand ValidCommand() => new(
        "Backend Engineer",
        "Acme Corp",
        "Build and maintain backend services.",
        EmploymentType.FullTime,
        ExperienceLevel.MidLevel,
        "Dhaka",
        WorkArrangement.Remote,
        new List<string> { "C#", "SQL" },
        2,
        DateTime.UtcNow.AddDays(30),
        false
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
    [InlineData("Q")]
    public void Validate_WithInvalidTitle_HasError(string? title)
    {
        var command = ValidCommand() with { Title = title! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_WithTitleOver200Characters_HasError()
    {
        var command = ValidCommand() with { Title = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_WithTitleStartingWithADigit_HasError()
    {
        var command = ValidCommand() with { Title = "1 QA Engineer" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title)
            .WithErrorMessage("Title cannot start with a number.");
    }

    [Fact]
    public void Validate_WithTitleContainingNoLetters_HasError()
    {
        var command = ValidCommand() with { Title = "12345678" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title)
            .WithErrorMessage("Title must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithTitleThatIsWhitespaceOnly_DoesNotDoubleReportContentErrors()
    {
        var command = ValidCommand() with { Title = "     " };

        var result = _validator.TestValidate(command);

        result.Errors.Where(e => e.PropertyName == nameof(CreateJobCommand.Title))
            .Should().ContainSingle(e => e.ErrorMessage == "Title must be at least 2 characters.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("A")]
    public void Validate_WithInvalidCompanyName_HasError(string? companyName)
    {
        var command = ValidCommand() with { CompanyName = companyName! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName);
    }

    [Fact]
    public void Validate_WithCompanyNameOver200Characters_HasError()
    {
        var command = ValidCommand() with { CompanyName = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName);
    }

    [Fact]
    public void Validate_WithCompanyNameContainingNoLetters_HasError()
    {
        var command = ValidCommand() with { CompanyName = "----" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName)
            .WithErrorMessage("Company name must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithShortRealWorldCompanyName_HasNoError()
    {
        var command = ValidCommand() with { CompanyName = "IBM" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.CompanyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Too short.")]
    public void Validate_WithInvalidDescription_HasError(string? description)
    {
        var command = ValidCommand() with { Description = description! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WithDescriptionOver10000Characters_HasError()
    {
        var command = ValidCommand() with { Description = new string('a', 10001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WithDescriptionThatIsAllWhitespace_HasError()
    {
        var command = ValidCommand() with { Description = new string(' ', 25) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description)
            .WithErrorMessage("Description must be at least 20 characters.");
    }

    [Fact]
    public void Validate_WithDescriptionContainingNoLetters_HasError()
    {
        var command = ValidCommand() with { Description = "1234567890 1234567890" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description)
            .WithErrorMessage("Description must contain at least one letter.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N")]
    public void Validate_WithInvalidLocation_HasError(string? location)
    {
        var command = ValidCommand() with { Location = location! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithLocationOver200Characters_HasError()
    {
        var command = ValidCommand() with { Location = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithPurelyNumericLocation_HasError()
    {
        var command = ValidCommand() with { Location = "12345" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location)
            .WithErrorMessage("Location cannot be purely numeric.");
    }

    [Fact]
    public void Validate_WithLocationMixingLettersAndDigits_HasNoError()
    {
        var command = ValidCommand() with { Location = "Dhaka-1205" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithUndefinedEmploymentType_HasError()
    {
        var command = ValidCommand() with { EmploymentType = (EmploymentType)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmploymentType);
    }

    [Fact]
    public void Validate_WithUndefinedExperienceLevel_HasError()
    {
        var command = ValidCommand() with { ExperienceLevel = (ExperienceLevel)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExperienceLevel);
    }

    [Fact]
    public void Validate_WithUndefinedWorkArrangement_HasError()
    {
        var command = ValidCommand() with { WorkArrangement = (WorkArrangement)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.WorkArrangement);
    }

    [Fact]
    public void Validate_WithNegativeVacancies_HasError()
    {
        var command = ValidCommand() with { Vacancies = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Vacancies);
    }

    [Fact]
    public void Validate_WithZeroVacancies_HasNoError()
    {
        var command = ValidCommand() with { Vacancies = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Vacancies);
    }

    [Fact]
    public void Validate_WithABlankSkillEntry_HasError()
    {
        var command = ValidCommand() with { RequiredSkills = new List<string> { "C#", "   " } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[1]");
    }

    [Fact]
    public void Validate_WithEmptyRequiredSkillsList_HasNoError()
    {
        var command = ValidCommand() with { RequiredSkills = new List<string>() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.RequiredSkills);
    }

    [Fact]
    public void Validate_WithASkillThatIsASingleDigit_HasError()
    {
        var command = ValidCommand() with { RequiredSkills = new List<string> { "1" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[0]")
            .WithErrorMessage("Skill entries must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithASkillMadeEntirelyOfPunctuation_HasError()
    {
        var command = ValidCommand() with { RequiredSkills = new List<string> { "\"\"\"\"" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[0]")
            .WithErrorMessage("Skill entries must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithASingleLetterSkill_HasNoError()
    {
        var command = ValidCommand() with { RequiredSkills = new List<string> { "R" } };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("RequiredSkills[0]");
    }

    [Fact]
    public void Validate_WithExpireAtInThePast_HasError()
    {
        var command = ValidCommand() with { ExpireAt = DateTime.UtcNow.AddDays(-1) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpireAt)
            .WithErrorMessage("Expire date must be in the future.");
    }

    [Fact]
    public void Validate_WithExpireAtInTheFuture_HasNoError()
    {
        var command = ValidCommand() with { ExpireAt = DateTime.UtcNow.AddDays(1) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ExpireAt);
    }
}

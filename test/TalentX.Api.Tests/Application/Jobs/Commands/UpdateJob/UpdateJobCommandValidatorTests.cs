using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Jobs.Commands.UpdateJob;
using TalentX.Domain.Enums;

namespace TalentX.Api.Tests.Application.Jobs.Commands.UpdateJob;

public class UpdateJobCommandValidatorTests
{
    private readonly UpdateJobCommandValidator _validator = new();

    private static UpdateJobCommand EmptyCommand(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        null, null, null, null, null, null, null, null, null, null
    );

    [Fact]
    public void Validate_WithAllFieldsNull_HasNoErrors()
    {
        var result = _validator.TestValidate(EmptyCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("QA")]
    [InlineData("")]
    public void Validate_WithTitleTooShort_HasError(string title)
    {
        var command = EmptyCommand() with { Title = title };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_WithTitleOver200Characters_HasError()
    {
        var command = EmptyCommand() with { Title = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_WithTitleStartingWithADigit_HasError()
    {
        var command = EmptyCommand() with { Title = "1 QA Engineer" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title)
            .WithErrorMessage("Title cannot start with a number.");
    }

    [Fact]
    public void Validate_WithTitleContainingNoLetters_HasError()
    {
        var command = EmptyCommand() with { Title = "12345678" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title)
            .WithErrorMessage("Title must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithValidTitle_HasNoError()
    {
        var command = EmptyCommand() with { Title = "Staff Engineer" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_WithCompanyNameTooShort_HasError()
    {
        var command = EmptyCommand() with { CompanyName = "A" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName);
    }

    [Fact]
    public void Validate_WithCompanyNameOver200Characters_HasError()
    {
        var command = EmptyCommand() with { CompanyName = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName);
    }

    [Fact]
    public void Validate_WithCompanyNameContainingNoLetters_HasError()
    {
        var command = EmptyCommand() with { CompanyName = "----" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CompanyName)
            .WithErrorMessage("Company name must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithValidCompanyName_HasNoError()
    {
        var command = EmptyCommand() with { CompanyName = "Globex" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.CompanyName);
    }

    [Fact]
    public void Validate_WithDescriptionTooShort_HasError()
    {
        var command = EmptyCommand() with { Description = "Too short." };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WithDescriptionOver10000Characters_HasError()
    {
        var command = EmptyCommand() with { Description = new string('a', 10001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WithDescriptionThatIsAllWhitespace_HasError()
    {
        var command = EmptyCommand() with { Description = new string(' ', 25) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description)
            .WithErrorMessage("Description must be at least 20 characters.");
    }

    [Fact]
    public void Validate_WithDescriptionContainingNoLetters_HasError()
    {
        var command = EmptyCommand() with { Description = "1234567890 1234567890" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description)
            .WithErrorMessage("Description must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithValidDescription_HasNoError()
    {
        var command = EmptyCommand() with { Description = "Updated description with enough length." };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WithLocationTooShort_HasError()
    {
        var command = EmptyCommand() with { Location = "NY" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithLocationOver200Characters_HasError()
    {
        var command = EmptyCommand() with { Location = new string('a', 201) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithPurelyNumericLocation_HasError()
    {
        var command = EmptyCommand() with { Location = "12345" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Location)
            .WithErrorMessage("Location cannot be purely numeric.");
    }

    [Fact]
    public void Validate_WithValidLocation_HasNoError()
    {
        var command = EmptyCommand() with { Location = "Chattogram" };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public void Validate_WithUndefinedEmploymentType_HasError()
    {
        var command = EmptyCommand() with { EmploymentType = (EmploymentType)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmploymentType);
    }

    [Fact]
    public void Validate_WithDefinedEmploymentType_HasNoError()
    {
        var command = EmptyCommand() with { EmploymentType = EmploymentType.Contract };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.EmploymentType);
    }

    [Fact]
    public void Validate_WithUndefinedExperienceLevel_HasError()
    {
        var command = EmptyCommand() with { ExperienceLevel = (ExperienceLevel)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExperienceLevel);
    }

    [Fact]
    public void Validate_WithUndefinedWorkArrangement_HasError()
    {
        var command = EmptyCommand() with { WorkArrangement = (WorkArrangement)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.WorkArrangement);
    }

    [Fact]
    public void Validate_WithNegativeVacancies_HasError()
    {
        var command = EmptyCommand() with { Vacancies = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Vacancies);
    }

    [Fact]
    public void Validate_WithNonNegativeVacancies_HasNoError()
    {
        var command = EmptyCommand() with { Vacancies = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Vacancies);
    }

    [Fact]
    public void Validate_WithABlankSkillEntry_HasError()
    {
        var command = EmptyCommand() with { RequiredSkills = new List<string> { "C#", "   " } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[1]");
    }

    [Fact]
    public void Validate_WithExpireAtInThePast_HasError()
    {
        var command = EmptyCommand() with { ExpireAt = DateTime.UtcNow.AddDays(-1) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpireAt)
            .WithErrorMessage("Expire date must be in the future.");
    }

    [Fact]
    public void Validate_WithExpireAtInTheFuture_HasNoError()
    {
        var command = EmptyCommand() with { ExpireAt = DateTime.UtcNow.AddDays(1) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ExpireAt);
    }

    [Fact]
    public void Validate_WithExpireAtNull_HasNoError()
    {
        var result = _validator.TestValidate(EmptyCommand());

        result.ShouldNotHaveValidationErrorFor(c => c.ExpireAt);
    }
}

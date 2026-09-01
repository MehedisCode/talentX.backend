using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.UpdateJob;

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
    [InlineData("Q")]
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
        var command = EmptyCommand() with { Location = "N" };

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
    public void Validate_WithASkillThatIsASingleDigit_HasError()
    {
        var command = EmptyCommand() with { RequiredSkills = new List<string> { "1" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[0]")
            .WithErrorMessage("Skill entries must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithASkillMadeEntirelyOfPunctuation_HasError()
    {
        var command = EmptyCommand() with { RequiredSkills = new List<string> { "\"\"\"\"" } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("RequiredSkills[0]")
            .WithErrorMessage("Skill entries must contain at least one letter.");
    }

    [Fact]
    public void Validate_WithASingleLetterSkill_HasNoError()
    {
        var command = EmptyCommand() with { RequiredSkills = new List<string> { "R" } };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("RequiredSkills[0]");
    }

    [Fact]
    public void Validate_WithExpireAtInThePast_HasError()
    {
        var command = EmptyCommand() with { ExpireAt = DateTime.UtcNow.AddDays(-1) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpireAt)
            .WithErrorMessage("Expire date must be today or in the future.");
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

    [Fact]
    public void Validate_OnSiteWithCityAndCountry_HasNoErrors()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = "Dhaka",
            Country = "Bangladesh"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OnSiteWithNoCity_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = null,
            Country = "Bangladesh"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_OnSiteWithNoCountry_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = "Dhaka",
            Country = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Country);
    }

    [Fact]
    public void Validate_HybridWithEmptyCity_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.Hybrid,
            City = "   ",
            Country = "Bangladesh"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.City);
    }

    [Fact]
    public void Validate_HybridWithEmptyCountry_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.Hybrid,
            City = "Dhaka",
            Country = ""
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Country);
    }

    [Fact]
    public void Validate_AnywhereFullRemoteWithNoCity_HasNoCityError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.FullRemote,
            City = null,
            Country = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.City);
        result.ShouldNotHaveValidationErrorFor(c => c.Country);
    }

    [Fact]
    public void Validate_AnywhereFullRemoteWithNoMinOverlapHours_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.FullRemote,
            City = null,
            Country = null,
            MinOverlapHours = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.MinOverlapHours);
    }

    [Fact]
    public void Validate_AnywhereFullRemoteWithZeroMinOverlapHours_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.FullRemote,
            City = null,
            Country = null,
            MinOverlapHours = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.MinOverlapHours);
    }

    [Fact]
    public void Validate_AnywhereFullRemoteWithValidMinOverlapHours_HasNoError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.FullRemote,
            City = null,
            Country = null,
            MinOverlapHours = 4
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.MinOverlapHours);
    }

    [Fact]
    public void Validate_OnSiteWithCityOver100Chars_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = new string('a', 101),
            Country = "Bangladesh"
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.City)
            .WithErrorMessage("City must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_OnSiteWithCountryOver100Chars_HasError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = WorkArrangement.OnSite,
            City = "Dhaka",
            Country = new string('b', 101)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Country)
            .WithErrorMessage("Country must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_NullWorkArrangementWithCity_HasNoCityError()
    {
        var command = EmptyCommand() with
        {
            WorkArrangement = null,
            City = "Dhaka",
            Country = "Bangladesh"
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.City);
        result.ShouldNotHaveValidationErrorFor(c => c.Country);
    }
}

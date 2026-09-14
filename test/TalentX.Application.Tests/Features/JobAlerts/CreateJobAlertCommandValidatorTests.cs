using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class CreateJobAlertCommandValidatorTests
{
    private readonly CreateJobAlertCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveErrors()
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Valid Alert",
            Title: "Software Engineer",
            Location: "Berlin",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.SeniorLevel,
            WorkArrangement: WorkArrangement.FullRemote);

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_ShouldHaveValidationError(string? invalidName)
    {
        var parameters = new CreateJobAlertParameters(
            Name: invalidName!,
            Title: "Valid Title");

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Name);
    }

    [Fact]
    public void Validate_NameExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new CreateJobAlertParameters(
            Name: new string('A', 201),
            Title: "Valid Title");

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTitle_ShouldHaveValidationError(string? invalidTitle)
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Valid Name",
            Title: invalidTitle!);

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Title);
    }

    [Fact]
    public void Validate_TitleExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Valid Name",
            Title: new string('B', 201));

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Title);
    }

    [Fact]
    public void Validate_LocationExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new CreateJobAlertParameters(
            Name: "Valid Name",
            Title: "Valid Title",
            Location: new string('C', 201));

        var command = new CreateJobAlertCommand(parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Location);
    }
}

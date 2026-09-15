using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class UpdateJobAlertCommandValidatorTests
{
    private readonly UpdateJobAlertCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveErrors()
    {
        var parameters = new UpdateJobAlertParameters(
            Name: "Updated Name",
            Title: "Staff Engineer",
            Location: "Amsterdam",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.Lead,
            WorkArrangement: WorkArrangement.Hybrid);

        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldHaveValidationError()
    {
        var parameters = new UpdateJobAlertParameters("Valid Name", "Valid Title");
        var command = new UpdateJobAlertCommand(Guid.Empty, parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_ShouldHaveValidationError(string? invalidName)
    {
        var parameters = new UpdateJobAlertParameters(invalidName!, "Valid Title");
        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Name);
    }

    [Fact]
    public void Validate_NameExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new UpdateJobAlertParameters(new string('A', 201), "Valid Title");
        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTitle_ShouldHaveValidationError(string? invalidTitle)
    {
        var parameters = new UpdateJobAlertParameters("Valid Name", invalidTitle!);
        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Title);
    }

    [Fact]
    public void Validate_TitleExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new UpdateJobAlertParameters("Valid Name", new string('B', 201));
        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Title);
    }

    [Fact]
    public void Validate_LocationExceeds200Characters_ShouldHaveValidationError()
    {
        var parameters = new UpdateJobAlertParameters("Valid Name", "Valid Title", new string('C', 201));
        var command = new UpdateJobAlertCommand(Guid.NewGuid(), parameters);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parameters.Location);
    }
}

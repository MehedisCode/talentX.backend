using FluentValidation.TestHelper;
using TalentX.Application.Features.JobReports.Commands.ResolveJobReport;
using TalentX.Domain.Enums;
using Xunit;
using System;

namespace TalentX.Application.Tests.Features.JobReports.Commands.ResolveJobReport;

public class ResolveJobReportCommandValidatorTests
{
    private readonly ResolveJobReportCommandValidator _validator;

    public ResolveJobReportCommandValidatorTests()
    {
        _validator = new ResolveJobReportCommandValidator();
    }

    private ResolveJobReportCommand ValidCommand()
    {
        return new ResolveJobReportCommand(Guid.NewGuid(), DeactivationReason.Spam, "This job is definitely spam.", "Resolved by admin.");
    }

    [Fact]
    public void ReportId_EmptyState_HasValidationError()
    {
        var command = ValidCommand() with { ReportId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ReportId);
    }

    [Fact]
    public void DeactivationReason_InvalidState_HasValidationError()
    {
        var command = ValidCommand() with { DeactivationReason = (DeactivationReason)999 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.DeactivationReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    public void Details_WhenEmptyOrTooShort_HasValidationError(string details)
    {
        var command = ValidCommand() with { Details = details };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Details);
    }

    [Fact]
    public void Details_TooLongState_HasValidationError()
    {
        var command = ValidCommand() with { Details = new string('a', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Details);
    }

    [Fact]
    public void AdminNotes_TooLongState_HasValidationError()
    {
        var command = ValidCommand() with { AdminNotes = new string('a', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdminNotes);
    }

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

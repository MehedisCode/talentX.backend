using FluentValidation.TestHelper;
using TalentX.Application.Features.JobReports.Commands.DismissJobReport;
using Xunit;
using System;

namespace TalentX.Application.Tests.Features.JobReports.Commands.DismissJobReport;

public class DismissJobReportCommandValidatorTests
{
    private readonly DismissJobReportCommandValidator _validator;

    public DismissJobReportCommandValidatorTests()
    {
        _validator = new DismissJobReportCommandValidator();
    }

    private DismissJobReportCommand ValidCommand()
    {
        return new DismissJobReportCommand(Guid.NewGuid(), "This is a valid note.");
    }

    [Fact]
    public void ReportId_WhenEmpty_HasValidationError()
    {
        var command = ValidCommand() with { ReportId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ReportId);
    }

    [Fact]
    public void AdminNotes_WhenTooLong_HasValidationError()
    {
        var command = ValidCommand() with { AdminNotes = new string('a', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdminNotes);
    }

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

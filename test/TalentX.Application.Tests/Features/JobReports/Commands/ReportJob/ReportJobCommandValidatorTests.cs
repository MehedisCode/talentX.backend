using FluentValidation.TestHelper;
using TalentX.Application.Features.JobReports.Commands.ReportJob;
using TalentX.Domain.Enums;
using Xunit;
using System;

namespace TalentX.Application.Tests.Features.JobReports.Commands.ReportJob;

public class ReportJobCommandValidatorTests
{
    private readonly ReportJobCommandValidator _validator;

    public ReportJobCommandValidatorTests()
    {
        _validator = new ReportJobCommandValidator();
    }

    private ReportJobCommand ValidCommand()
    {
        return new ReportJobCommand(Guid.NewGuid(), JobReportReason.SuspectedScam, "This is a spam post.");
    }

    [Fact]
    public void JobId_WhenEmpty_HasValidationError()
    {
        var command = ValidCommand() with { JobId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.JobId);
    }

    [Fact]
    public void Reason_WhenInvalidEnum_HasValidationError()
    {
        var command = ValidCommand() with { Reason = (JobReportReason)999 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Reason_WhenOtherAndAdditionalDetailsEmpty_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.Other, AdditionalDetails = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void Reason_WhenOtherAndAdditionalDetailsTooShort_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.Other, AdditionalDetails = "abc" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void Reason_WhenNotOtherAndAdditionalDetailsProvidedButTooShort_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.SuspectedScam, AdditionalDetails = "abc" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void AdditionalDetails_WhenExceeds1000Chars_HasValidationError()
    {
        var command = ValidCommand() with { AdditionalDetails = new string('a', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

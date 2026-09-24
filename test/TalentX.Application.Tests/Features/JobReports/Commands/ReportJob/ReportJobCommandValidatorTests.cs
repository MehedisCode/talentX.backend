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
    public void JobId_EmptyState_HasValidationError()
    {
        var command = ValidCommand() with { JobId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.JobId);
    }

    [Fact]
    public void Reason_InvalidEnumState_HasValidationError()
    {
        var command = ValidCommand() with { Reason = (JobReportReason)999 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Validate_ReasonOtherAndAdditionalDetailsEmpty_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.Other, AdditionalDetails = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void Validate_ReasonOtherAndAdditionalDetailsTooShort_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.Other, AdditionalDetails = "abc" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void Validate_ReasonNotOtherAndAdditionalDetailsProvidedButTooShort_HasValidationError()
    {
        var command = ValidCommand() with { Reason = JobReportReason.SuspectedScam, AdditionalDetails = "abc" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void AdditionalDetails_Exceeds1000CharsState_HasValidationError()
    {
        var command = ValidCommand() with { AdditionalDetails = new string('a', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.AdditionalDetails);
    }

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = ValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

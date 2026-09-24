using FluentValidation.TestHelper;
using TalentX.Application.Features.JobApplications.Commands.UpdateApplicationStatus;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.UpdateApplicationStatus;

public class UpdateApplicationStatusCommandValidatorTests
{
    private readonly UpdateApplicationStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(ApplicationStatus.Reviewed)]
    [InlineData(ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Rejected)]
    public void Validate_ManuallyAssignableStatus_ShouldNotHaveValidationError(ApplicationStatus status)
    {
        var command = new UpdateApplicationStatusCommand(Guid.NewGuid(), status);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.NewStatus);
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.PendingVerification)]
    [InlineData(ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Invited)]
    public void Validate_SystemManagedStatus_ShouldHaveValidationError(ApplicationStatus status)
    {
        var command = new UpdateApplicationStatusCommand(Guid.NewGuid(), status);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.NewStatus);
    }
}

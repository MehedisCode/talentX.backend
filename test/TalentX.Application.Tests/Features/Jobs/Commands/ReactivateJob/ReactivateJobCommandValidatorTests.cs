using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.ReactivateJob;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ReactivateJob;

public sealed class ReactivateJobCommandValidatorTests
{
    private readonly ReactivateJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WithJobId_HasNoValidationErrors()
    {
        _validator.TestValidate(new ReactivateJobCommand(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenJobIdIsEmpty_HasValidationError()
    {
        // The {id:guid} route accepts 00000000-0000-0000-0000-000000000000, so this must be
        // rejected here as a bad request rather than reaching the database as a 404.
        _validator.TestValidate(new ReactivateJobCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.JobId)
            .WithErrorMessage("Job ID is required.");
    }
}

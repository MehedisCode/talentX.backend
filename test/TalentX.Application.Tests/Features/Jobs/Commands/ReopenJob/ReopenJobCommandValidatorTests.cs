using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.ReopenJob;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ReopenJob;

public sealed class ReopenJobCommandValidatorTests
{
    private readonly ReopenJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidJobId_HasNoValidationErrors()
    {
        var command = new ReopenJobCommand(Guid.NewGuid());

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyJobId_HasValidationError()
    {
        var command = new ReopenJobCommand(Guid.Empty);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(targetCommand => targetCommand.Id)
            .WithErrorMessage("Job ID is required.");
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.CloseJob;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.CloseJob;

public sealed class CloseJobCommandValidatorTests
{
    private readonly CloseJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidJobId_HasNoValidationErrors()
    {
        var command = new CloseJobCommand(Guid.NewGuid());

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyJobId_HasValidationError()
    {
        var command = new CloseJobCommand(Guid.Empty);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.Id)
            .WithErrorMessage("Job ID is required.");
    }
}

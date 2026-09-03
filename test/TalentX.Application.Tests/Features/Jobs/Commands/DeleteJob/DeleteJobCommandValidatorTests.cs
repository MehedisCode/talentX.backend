using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.Jobs.Commands.DeleteJob;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.DeleteJob;

public sealed class DeleteJobCommandValidatorTests
{
    private readonly DeleteJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidJobId_HasNoValidationErrors()
    {
        var command = new DeleteJobCommand(Guid.NewGuid());

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyJobId_HasValidationError()
    {
        var command = new DeleteJobCommand(Guid.Empty);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.Id)
            .WithErrorMessage("Job ID is required.");
    }
}

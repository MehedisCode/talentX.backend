using FluentValidation.TestHelper;
using TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Users.Commands.UpdateUserAccountStatus;

public sealed class UpdateUserAccountStatusCommandValidatorTests
{
    private readonly UpdateUserAccountStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoValidationErrors()
    {
        var command = new UpdateUserAccountStatusCommand(Guid.NewGuid(), AccountStatus.Deactivated);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_HasValidationError()
    {
        var command = new UpdateUserAccountStatusCommand(Guid.Empty, AccountStatus.Active);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorMessage("User ID is required.");
    }

    [Fact]
    public void Validate_WithInvalidNewStatus_HasValidationError()
    {
        var command = new UpdateUserAccountStatusCommand(Guid.NewGuid(), (AccountStatus)999);

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.NewStatus)
            .WithErrorMessage("Invalid account status.");
    }
}

using FluentValidation.TestHelper;
using TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.RegeneratePreparation;

public class RegeneratePreparationCommandValidatorTests
{
    private readonly RegeneratePreparationCommandValidator _validator = new();

    [Fact]
    public void InvitationId_EmptyState_HasValidationError()
    {
        var command = new RegeneratePreparationCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.InvitationId)
            .WithErrorMessage("InvitationId is required.");
    }

    [Fact]
    public void InvitationId_ValidState_PassesValidation()
    {
        var command = new RegeneratePreparationCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

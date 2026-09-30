using FluentValidation.TestHelper;
using TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.GeneratePreparation;

public class GeneratePreparationCommandValidatorTests
{
    private readonly GeneratePreparationCommandValidator _validator = new();

    [Fact]
    public void InvitationId_EmptyState_HasValidationError()
    {
        var command = new GeneratePreparationCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.InvitationId);
    }

    [Fact]
    public void InvitationId_ValidState_PassesValidation()
    {
        var command = new GeneratePreparationCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

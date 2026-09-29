using FluentValidation.TestHelper;
using TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public class GenerateAdditionalQuestionsCommandValidatorTests
{
    private readonly GenerateAdditionalQuestionsCommandValidator _validator = new();

    [Fact]
    public void InvitationId_EmptyState_HasValidationError()
    {
        var command = new GenerateAdditionalQuestionsCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.InvitationId)
            .WithErrorMessage("InvitationId is required.");
    }

    [Fact]
    public void InvitationId_ValidState_PassesValidation()
    {
        var command = new GenerateAdditionalQuestionsCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

using FluentValidation.TestHelper;
using TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.EvaluateAnswers;

public class EvaluateAnswersCommandValidatorTests
{
    private readonly EvaluateAnswersCommandValidator _validator = new();

    [Fact]
    public void InvitationId_ValidState_PassesValidation()
    {
        var result = _validator.TestValidate(new EvaluateAnswersCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvitationId_EmptyState_HasValidationError()
    {
        var result = _validator.TestValidate(new EvaluateAnswersCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.InvitationId);
    }
}

using FluentValidation.TestHelper;
using TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation.Commands.SaveAnswer;

public class SaveAnswerCommandValidatorTests
{
    private readonly SaveAnswerCommandValidator _validator = new();

    private static SaveAnswerCommand ValidCommand() =>
        new(Guid.NewGuid(), "Technical", 0, "A closure captures its lexical scope.", true);

    [Fact]
    public void Command_ValidState_PassesValidation()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvitationId_EmptyState_HasValidationError()
    {
        var result = _validator.TestValidate(ValidCommand() with { InvitationId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.InvitationId);
    }

    [Theory]
    [InlineData("Technical")]
    [InlineData("Behavioral")]
    [InlineData("Experience-Based")]
    [InlineData("Role-Specific")]
    [InlineData("Additional")]
    public void Category_KnownCategory_PassesValidation(string category)
    {
        var result = _validator.TestValidate(ValidCommand() with { Category = category });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Category_UnknownCategory_HasValidationError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Category = "Nonsense" });

        result.ShouldHaveValidationErrorFor(c => c.Category)
            .WithErrorMessage("Category must be a known question category.");
    }

    [Fact]
    public void Answer_TooLong_HasValidationError()
    {
        var result = _validator.TestValidate(
            ValidCommand() with { Answer = new string('a', 4001) });

        result.ShouldHaveValidationErrorFor(c => c.Answer);
    }

    [Fact]
    public void QuestionIndex_OutOfRange_HasValidationError()
    {
        var result = _validator.TestValidate(ValidCommand() with { QuestionIndex = 10_001 });

        result.ShouldHaveValidationErrorFor(c => c.QuestionIndex);
    }
}

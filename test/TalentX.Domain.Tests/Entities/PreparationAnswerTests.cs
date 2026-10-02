using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class PreparationAnswerTests
{
    private static CreatePreparationAnswerPayload Payload(
        string answerText = "My answer",
        bool isPracticed = false)
    {
        return new CreatePreparationAnswerPayload(
            Guid.NewGuid(), "Technical", 0, "What is a closure?", answerText, isPracticed);
    }

    [Fact]
    public void Create_ValidArguments_SetsAllFields()
    {
        var payload = Payload(answerText: "A function over its scope", isPracticed: true);

        var answer = PreparationAnswer.Create(payload);

        answer.InterviewPreparationId.Should().Be(payload.InterviewPreparationId);
        answer.Category.Should().Be("Technical");
        answer.QuestionIndex.Should().Be(0);
        answer.QuestionText.Should().Be("What is a closure?");
        answer.AnswerText.Should().Be("A function over its scope");
        answer.IsPracticed.Should().BeTrue();
        answer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        answer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_WithNewContent_UpdatesFieldsAndSetsUpdatedAt()
    {
        var answer = PreparationAnswer.Create(Payload(answerText: "Old"));

        answer.Update(new UpdatePreparationAnswerPayload(
            "What is hoisting?", "New answer", IsPracticed: true));

        answer.QuestionText.Should().Be("What is hoisting?");
        answer.AnswerText.Should().Be("New answer");
        answer.IsPracticed.Should().BeTrue();
        answer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("Real answer", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void HasAnswer_DependsOnWhitespaceOnlyText(string answerText, bool expected)
    {
        var answer = PreparationAnswer.Create(Payload(answerText: answerText));

        answer.HasAnswer.Should().Be(expected);
    }
}

using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class InterviewPreparationEvaluationTests
{
    private static CreateEvaluationPayload Payload(
        int overallScore = 75,
        int answeredCount = 3,
        int totalCount = 10)
    {
        return new CreateEvaluationPayload(
            Guid.NewGuid(),
            overallScore,
            "Solid effort.",
            """["Clear structure"]""",
            """["More metrics"]""",
            """[{"category":"Technical","questionIndex":0,"score":70,"feedback":"Good.","suggestedAnswer":""}]""",
            answeredCount,
            totalCount);
    }

    [Fact]
    public void Create_ValidArguments_SetsAllFieldsAndEvaluatedAt()
    {
        var payload = Payload();

        var evaluation = InterviewPreparationEvaluation.Create(payload);

        evaluation.InterviewPreparationId.Should().Be(payload.InterviewPreparationId);
        evaluation.OverallScore.Should().Be(75);
        evaluation.Summary.Should().Be("Solid effort.");
        evaluation.Strengths.Should().Be("""["Clear structure"]""");
        evaluation.AreasToImprove.Should().Be("""["More metrics"]""");
        evaluation.QuestionFeedback.Should().Contain("questionIndex");
        evaluation.AnsweredCount.Should().Be(3);
        evaluation.TotalCount.Should().Be(10);
        evaluation.EvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        evaluation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(150, 100)]
    public void Create_WhenScoreOutOfRange_ClampsIt(int rawScore, int expected)
    {
        var evaluation = InterviewPreparationEvaluation.Create(Payload(overallScore: rawScore));

        evaluation.OverallScore.Should().Be(expected);
    }

    [Fact]
    public void Replace_WithNewPayload_OverwritesContentAndRefreshesEvaluatedAt()
    {
        var evaluation = InterviewPreparationEvaluation.Create(Payload(overallScore: 40));
        Thread.Sleep(10);

        evaluation.Replace(Payload(overallScore: 88, answeredCount: 5, totalCount: 12));

        evaluation.OverallScore.Should().Be(88);
        evaluation.AnsweredCount.Should().Be(5);
        evaluation.TotalCount.Should().Be(12);
        evaluation.EvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        evaluation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class InterviewPreparationEvaluation : BaseEntity
{
    public Guid InterviewPreparationId { get; private set; }
    public int OverallScore { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Strengths { get; private set; } = "[]";
    public string AreasToImprove { get; private set; } = "[]";
    public string QuestionFeedback { get; private set; } = "[]";
    public int AnsweredCount { get; private set; }
    public int TotalCount { get; private set; }
    public DateTime EvaluatedAt { get; private set; }

    private InterviewPreparationEvaluation() { }

    public static InterviewPreparationEvaluation Create(CreateEvaluationPayload payload)
    {
        var evaluation = new InterviewPreparationEvaluation
        {
            InterviewPreparationId = payload.InterviewPreparationId,
            CreatedAt = DateTime.UtcNow
        };
        evaluation.Apply(payload);
        return evaluation;
    }

    public void Replace(CreateEvaluationPayload payload)
    {
        Apply(payload);
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(CreateEvaluationPayload payload)
    {
        OverallScore = Math.Clamp(payload.OverallScore, 0, 100);
        Summary = payload.Summary;
        Strengths = payload.Strengths;
        AreasToImprove = payload.AreasToImprove;
        QuestionFeedback = payload.QuestionFeedback;
        AnsweredCount = payload.AnsweredCount;
        TotalCount = payload.TotalCount;
        EvaluatedAt = DateTime.UtcNow;
    }
}

using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class PreparationAnswer : BaseEntity
{
    public Guid InterviewPreparationId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public int QuestionIndex { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public string AnswerText { get; private set; } = string.Empty;
    public bool IsPracticed { get; private set; }

    private PreparationAnswer() { }

    public static PreparationAnswer Create(CreatePreparationAnswerPayload payload)
    {
        return new PreparationAnswer
        {
            InterviewPreparationId = payload.InterviewPreparationId,
            Category = payload.Category,
            QuestionIndex = payload.QuestionIndex,
            QuestionText = payload.QuestionText,
            AnswerText = payload.AnswerText,
            IsPracticed = payload.IsPracticed,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(UpdatePreparationAnswerPayload payload)
    {
        QuestionText = payload.QuestionText;
        AnswerText = payload.AnswerText;
        IsPracticed = payload.IsPracticed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasAnswer => !string.IsNullOrWhiteSpace(AnswerText);
}

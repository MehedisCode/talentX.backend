namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record PrepAnswerDto(
    string Category,
    int QuestionIndex,
    string Question,
    string Answer,
    bool Practiced);

public sealed record QuestionFeedbackDto(
    string Category,
    int QuestionIndex,
    int Score,
    string Feedback,
    string? SuggestedAnswer);

public sealed record EvaluationDto
{
    public int OverallScore { get; init; }
    public string Summary { get; init; } = string.Empty;
    public List<string> Strengths { get; init; } = new();
    public List<string> AreasToImprove { get; init; } = new();
    public List<QuestionFeedbackDto> QuestionFeedback { get; init; } = new();
    public int AnsweredCount { get; init; }
    public int TotalCount { get; init; }
    public DateTime EvaluatedAt { get; init; }
}

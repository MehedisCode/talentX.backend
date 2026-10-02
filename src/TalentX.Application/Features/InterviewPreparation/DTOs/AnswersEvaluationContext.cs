namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record AnswersEvaluationContext
{
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string TalentName { get; init; } = string.Empty;
    public string InterviewType { get; init; } = string.Empty;
    public int TotalQuestionCount { get; init; }
    public List<AnsweredQuestion> AnsweredQuestions { get; init; } = new();
}

public sealed record AnsweredQuestion(
    string Category,
    int QuestionIndex,
    string Question,
    string Answer);

public sealed record AnswersEvaluationResult
{
    public int OverallScore { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string Strengths { get; init; } = "[]";
    public string AreasToImprove { get; init; } = "[]";
    public string QuestionFeedback { get; init; } = "[]";
}

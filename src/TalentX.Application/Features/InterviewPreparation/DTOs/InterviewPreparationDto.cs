namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record InterviewPreparationDto
{
    public Guid Id { get; init; }
    public Guid InterviewInvitationId { get; init; }
    public Guid JobId { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string InterviewType { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
    public string Status { get; init; } = string.Empty;

    public List<PrepQuestion> TechnicalQuestions { get; init; } = new();
    public List<PrepQuestion> BehavioralQuestions { get; init; } = new();
    public List<PrepQuestion> ExperienceBasedQuestions { get; init; } = new();
    public List<PrepQuestion> RoleSpecificQuestions { get; init; } = new();
    public List<string> KeyTopicsToPrepare { get; init; } = new();
    public List<string> SuggestedRevisionAreas { get; init; } = new();
    public List<PrepQuestion> AdditionalQuestions { get; init; } = new();

    public int GenerationCount { get; init; }
    public DateTime GeneratedAt { get; init; }
    public bool IsAccessible { get; init; }

    public List<PrepAnswerDto> Answers { get; init; } = new();
    public EvaluationDto? Evaluation { get; init; }

    public string Disclaimer { get; init; } =
        "This content is AI-generated preparation guidance and does not guarantee interview outcomes.";
}

public sealed record PrepQuestion(
    string Question, string Category, string? Tip);

namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record InterviewPreparationResult
{
    public string TechnicalQuestions { get; init; } = string.Empty;
    public string BehavioralQuestions { get; init; } = string.Empty;
    public string ExperienceBasedQuestions { get; init; } = string.Empty;
    public string RoleSpecificQuestions { get; init; } = string.Empty;
    public string KeyTopicsToPrepare { get; init; } = string.Empty;
    public string SuggestedRevisionAreas { get; init; } = string.Empty;
}

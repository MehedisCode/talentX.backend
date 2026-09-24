using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class InterviewPreparation : BaseEntity
{
    public Guid InterviewInvitationId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public Guid JobId { get; private set; }
    public string JobTitle { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public InterviewPreparationStatus Status { get; private set; }

    // Interview context
    public string InterviewType { get; private set; } = string.Empty;
    public DateTime? ScheduledAt { get; private set; }

    // AI-generated content (stored as JSON strings)
    public string TechnicalQuestions { get; private set; } = string.Empty;
    public string BehavioralQuestions { get; private set; } = string.Empty;
    public string ExperienceBasedQuestions { get; private set; } = string.Empty;
    public string RoleSpecificQuestions { get; private set; } = string.Empty;
    public string KeyTopicsToPrepare { get; private set; } = string.Empty;
    public string SuggestedRevisionAreas { get; private set; } = string.Empty;

    // Additional questions generated on demand
    public string? AdditionalQuestions { get; private set; }

    // Track regeneration count for rate limiting
    public int GenerationCount { get; private set; }

    private InterviewPreparation() { }

    public static InterviewPreparation Create(
        Guid interviewInvitationId,
        string talentUserId,
        Guid jobId,
        string jobTitle,
        string companyName,
        string interviewType,
        DateTime? scheduledAt,
        string technicalQuestions,
        string behavioralQuestions,
        string experienceBasedQuestions,
        string roleSpecificQuestions,
        string keyTopicsToPrepare,
        string suggestedRevisionAreas)
    {
        return new InterviewPreparation
        {
            InterviewInvitationId = interviewInvitationId,
            TalentUserId = talentUserId,
            JobId = jobId,
            JobTitle = jobTitle,
            CompanyName = companyName,
            InterviewType = interviewType,
            ScheduledAt = scheduledAt,
            Status = InterviewPreparationStatus.Generated,
            TechnicalQuestions = technicalQuestions,
            BehavioralQuestions = behavioralQuestions,
            ExperienceBasedQuestions = experienceBasedQuestions,
            RoleSpecificQuestions = roleSpecificQuestions,
            KeyTopicsToPrepare = keyTopicsToPrepare,
            SuggestedRevisionAreas = suggestedRevisionAreas,
            GenerationCount = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Regenerate(
        string technicalQuestions,
        string behavioralQuestions,
        string experienceBasedQuestions,
        string roleSpecificQuestions,
        string keyTopicsToPrepare,
        string suggestedRevisionAreas)
    {
        TechnicalQuestions = technicalQuestions;
        BehavioralQuestions = behavioralQuestions;
        ExperienceBasedQuestions = experienceBasedQuestions;
        RoleSpecificQuestions = roleSpecificQuestions;
        KeyTopicsToPrepare = keyTopicsToPrepare;
        SuggestedRevisionAreas = suggestedRevisionAreas;
        AdditionalQuestions = null; // Clear previous additional questions
        Status = InterviewPreparationStatus.Regenerated;
        GenerationCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdditionalQuestions(string additionalQuestions)
    {
        AdditionalQuestions = additionalQuestions;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the preparation is still accessible based on the interview schedule.
    /// Default: accessible until 30 minutes after the scheduled interview time.
    /// </summary>
    public bool IsAccessible(TimeSpan? accessWindowAfterInterview = null)
    {
        if (ScheduledAt is null) return true; // No schedule = always accessible
        var window = accessWindowAfterInterview ?? TimeSpan.FromMinutes(30);
        return DateTime.UtcNow <= ScheduledAt.Value.Add(window);
    }
}

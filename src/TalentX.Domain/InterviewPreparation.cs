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

    public string InterviewType { get; private set; } = string.Empty;
    public DateTime? ScheduledAt { get; private set; }

    // AI-generated content (stored as JSON strings)
    public string TechnicalQuestions { get; private set; } = string.Empty;
    public string BehavioralQuestions { get; private set; } = string.Empty;
    public string ExperienceBasedQuestions { get; private set; } = string.Empty;
    public string RoleSpecificQuestions { get; private set; } = string.Empty;
    public string KeyTopicsToPrepare { get; private set; } = string.Empty;
    public string SuggestedRevisionAreas { get; private set; } = string.Empty;

    public string? AdditionalQuestions { get; private set; }

    public int GenerationCount { get; private set; }

    private InterviewPreparation() { }

    public static InterviewPreparation Create(CreateInterviewPreparationPayload payload)
    {
        return new InterviewPreparation
        {
            InterviewInvitationId = payload.InterviewInvitationId,
            TalentUserId = payload.TalentUserId,
            JobId = payload.JobId,
            JobTitle = payload.JobTitle,
            CompanyName = payload.CompanyName,
            InterviewType = payload.InterviewType,
            ScheduledAt = payload.ScheduledAt,
            Status = InterviewPreparationStatus.Generated,
            TechnicalQuestions = payload.TechnicalQuestions,
            BehavioralQuestions = payload.BehavioralQuestions,
            ExperienceBasedQuestions = payload.ExperienceBasedQuestions,
            RoleSpecificQuestions = payload.RoleSpecificQuestions,
            KeyTopicsToPrepare = payload.KeyTopicsToPrepare,
            SuggestedRevisionAreas = payload.SuggestedRevisionAreas,
            GenerationCount = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Regenerate(RegenerateInterviewPreparationPayload payload)
    {
        TechnicalQuestions = payload.TechnicalQuestions;
        BehavioralQuestions = payload.BehavioralQuestions;
        ExperienceBasedQuestions = payload.ExperienceBasedQuestions;
        RoleSpecificQuestions = payload.RoleSpecificQuestions;
        KeyTopicsToPrepare = payload.KeyTopicsToPrepare;
        SuggestedRevisionAreas = payload.SuggestedRevisionAreas;
        AdditionalQuestions = null;
        Status = InterviewPreparationStatus.Regenerated;
        GenerationCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdditionalQuestions(string additionalQuestions)
    {
        AdditionalQuestions = additionalQuestions;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsAccessible(TimeSpan accessWindowAfterInterview)
    {
        if (ScheduledAt is null) return true; // No schedule = always accessible
        return DateTime.UtcNow <= ScheduledAt.Value.Add(accessWindowAfterInterview);
    }
}

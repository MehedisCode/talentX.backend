namespace TalentX.Domain.Entities;

public sealed record CreateInterviewPreparationPayload(
    Guid InterviewInvitationId,
    string TalentUserId,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string InterviewType,
    DateTime? ScheduledAt,
    string TechnicalQuestions,
    string BehavioralQuestions,
    string ExperienceBasedQuestions,
    string RoleSpecificQuestions,
    string KeyTopicsToPrepare,
    string SuggestedRevisionAreas);

public sealed record RegenerateInterviewPreparationPayload(
    string TechnicalQuestions,
    string BehavioralQuestions,
    string ExperienceBasedQuestions,
    string RoleSpecificQuestions,
    string KeyTopicsToPrepare,
    string SuggestedRevisionAreas);

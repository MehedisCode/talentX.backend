namespace TalentX.Domain.Entities;

/// <summary>
/// Input fields for creating an <see cref="InterviewPreparation"/> — groups the
/// long argument list that would otherwise be passed to the factory method.
/// </summary>
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

/// <summary>
/// Fresh AI-generated content used when regenerating an existing preparation.
/// </summary>
public sealed record RegenerateInterviewPreparationPayload(
    string TechnicalQuestions,
    string BehavioralQuestions,
    string ExperienceBasedQuestions,
    string RoleSpecificQuestions,
    string KeyTopicsToPrepare,
    string SuggestedRevisionAreas);

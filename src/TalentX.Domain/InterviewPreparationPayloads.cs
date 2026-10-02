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

public sealed record CreatePreparationAnswerPayload(
    Guid InterviewPreparationId,
    string Category,
    int QuestionIndex,
    string QuestionText,
    string AnswerText,
    bool IsPracticed);

public sealed record UpdatePreparationAnswerPayload(
    string QuestionText,
    string AnswerText,
    bool IsPracticed);

public sealed record CreateEvaluationPayload(
    Guid InterviewPreparationId,
    int OverallScore,
    string Summary,
    string Strengths,
    string AreasToImprove,
    string QuestionFeedback,
    int AnsweredCount,
    int TotalCount);

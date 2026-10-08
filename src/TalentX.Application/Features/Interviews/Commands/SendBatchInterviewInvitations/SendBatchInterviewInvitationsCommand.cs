using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;

public sealed record TimeslotInput(DateTime StartTime, DateTime EndTime, string? MeetingUrl = null);

public sealed record CandidateInvitationResultDto(
    Guid ApplicationId,
    string CandidateName,
    string CandidateEmail,
    bool Success,
    Guid? InvitationId = null,
    string? SkipReason = null,
    bool CanForceSend = false,
    bool HasDanglingPendingInvite = false
);

public sealed record SendBatchInterviewInvitationsResult(
    int TotalProcessed,
    int InvitedCount,
    int SkippedCount,
    IReadOnlyList<CandidateInvitationResultDto> Results
);

public sealed record SendBatchInterviewInvitationsCommand(
    Guid JobId,
    List<Guid> ApplicationIds,
    InterviewType Type,
    string? AssessmentUrl = null,
    DateTime? AssessmentDeadline = null,
    string? LocationOrLink = null,
    string? Instructions = null,
    List<TimeslotInput>? Timeslots = null,
    bool Force = false
) : IRequest<SendBatchInterviewInvitationsResult>;

using MediatR;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;

public sealed record TimeslotInput(DateTime StartTime, DateTime EndTime, string? MeetingUrl = null);

public sealed record SendBatchInterviewInvitationsCommand(
    Guid JobId,
    List<Guid> ApplicationIds,
    InterviewType Type,
    string? AssessmentUrl = null,
    DateTime? AssessmentDeadline = null,
    string? LocationOrLink = null,
    string? Instructions = null,
    List<TimeslotInput>? Timeslots = null
) : IRequest<IReadOnlyList<Guid>>;

namespace TalentX.Application.Features.Interviews.DTOs;

public sealed record EmployerInterviewDto(
    Guid Id,
    Guid JobId,
    Guid JobApplicationId,
    string TalentUserId,
    string ApplicantName,
    string ApplicantEmail,
    string Type,
    string Status,
    string? AssessmentUrl,
    DateTime? AssessmentDeadline,
    string? LocationOrLink,
    string? Instructions,
    string? DeclineReason,
    Guid? SelectedTimeslotId,
    DateTime? ScheduledAt,
    DateTime SentAt,
    DateTime? RespondedAt,
    IReadOnlyList<InterviewTimeslotDto> Timeslots
);

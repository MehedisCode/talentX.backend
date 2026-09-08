namespace TalentX.Application.Features.Interviews.DTOs;

public sealed record TalentInterviewDto(
    Guid Id,
    Guid JobId,
    Guid JobApplicationId,
    string JobTitle,
    string CompanyName,
    string Type,
    string Status,
    string? AssessmentUrl,
    DateTime? AssessmentDeadline,
    string? LocationOrLink,
    string? Instructions,
    Guid? SelectedTimeslotId,
    DateTime? ScheduledAt,
    DateTime SentAt,
    DateTime? RespondedAt,
    IReadOnlyList<InterviewTimeslotDto> Timeslots
);

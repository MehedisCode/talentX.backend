namespace TalentX.Application.Features.Interviews.DTOs;

public sealed record InterviewTimeslotDto(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime,
    bool IsBooked,
    string? MeetingUrl = null
);

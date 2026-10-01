namespace TalentX.Application.Features.JobApplications.DTOs;

public sealed record ApplicationStatusHistoryDto(
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt);

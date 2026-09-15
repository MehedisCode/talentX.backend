using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobAlerts.DTOs;

public sealed record JobAlertDto(
    Guid Id,
    string Name,
    string Title,
    string? Location,
    EmploymentType? EmploymentType,
    ExperienceLevel? ExperienceLevel,
    WorkArrangement? WorkArrangement,
    bool IsEnabled,
    DateTime CreatedAt);

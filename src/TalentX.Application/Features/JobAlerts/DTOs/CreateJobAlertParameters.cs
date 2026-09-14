using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobAlerts.DTOs;

public sealed record CreateJobAlertParameters(
    string Name,
    string Title,
    string? Location = null,
    EmploymentType? EmploymentType = null,
    ExperienceLevel? ExperienceLevel = null,
    WorkArrangement? WorkArrangement = null);

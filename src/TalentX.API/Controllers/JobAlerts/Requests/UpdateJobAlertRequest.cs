using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobAlerts.Requests;

public sealed record UpdateJobAlertRequest(
    string Name,
    string Title,
    string? Location,
    EmploymentType? EmploymentType,
    ExperienceLevel? ExperienceLevel,
    WorkArrangement? WorkArrangement)
{
    public UpdateJobAlertParameters ToParameters() =>
        new(Name, Title, Location, EmploymentType, ExperienceLevel, WorkArrangement);
}

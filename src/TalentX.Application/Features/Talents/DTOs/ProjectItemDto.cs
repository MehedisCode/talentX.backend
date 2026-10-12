namespace TalentX.Application.Features.Talents.DTOs;

public sealed record ProjectItemDto(
    Guid Id,
    string ProjectName,
    string Role,
    string ProjectType,
    string StartDate,
    string? EndDate,
    bool IsOngoing,
    string Description,
    IReadOnlyList<string> TechnologiesUsed,
    string? ProjectUrl
);

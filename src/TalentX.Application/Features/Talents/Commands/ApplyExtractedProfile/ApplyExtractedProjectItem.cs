namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedProjectItem(
    string ProjectName,
    string Role,
    string ProjectType,
    string StartDate,
    string? EndDate,
    bool IsOngoing,
    string Description,
    List<string>? TechnologiesUsed,
    string? ProjectUrl
);

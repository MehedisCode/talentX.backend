namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedWorkExperienceItem(
    string CompanyName,
    string JobTitle,
    string? StartDate,
    string? EndDate,
    bool IsCurrent
);

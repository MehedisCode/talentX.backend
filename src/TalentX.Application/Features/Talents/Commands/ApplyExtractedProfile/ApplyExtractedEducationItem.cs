namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed record ApplyExtractedEducationItem(
    string Institution,
    string Degree,
    string? StartDate,
    string? EndDate
);

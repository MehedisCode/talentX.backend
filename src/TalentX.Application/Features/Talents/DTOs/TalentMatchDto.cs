namespace TalentX.Application.Features.Talents.DTOs;

public sealed record TalentMatchDto(
    Guid JobId,
    string JobTitle,
    string Company,
    int MatchScore);

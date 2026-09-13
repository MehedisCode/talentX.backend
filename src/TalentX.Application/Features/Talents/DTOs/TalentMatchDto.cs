namespace TalentX.Application.Features.Talents.DTOs;

public sealed record TalentMatchDto(
    Guid JobId,
    string JobTitle,
    string Company,
    int MatchScore,
    WorkArrangement WorkArrangement,
    EmploymentType EmploymentType,
    DateTime ExpireAt);

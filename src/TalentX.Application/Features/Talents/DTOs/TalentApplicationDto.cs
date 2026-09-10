namespace TalentX.Application.Features.Talents.DTOs;
public sealed record TalentApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime AppliedDate,
    string Status);

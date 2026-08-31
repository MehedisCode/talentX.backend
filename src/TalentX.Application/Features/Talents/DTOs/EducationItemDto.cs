namespace TalentX.Application.Features.Talents.DTOs;

public sealed record EducationItemDto(
    Guid Id,
    string Institution,
    string Degree,
    string? StartDate,
    string? EndDate
);

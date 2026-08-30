namespace TalentX.Application.Features.Talents.DTOs;

public sealed record WorkExperienceItemDto(
    Guid Id,
    string CompanyName,
    string JobTitle,
    string? StartDate,
    string? EndDate,
    bool IsCurrent
);

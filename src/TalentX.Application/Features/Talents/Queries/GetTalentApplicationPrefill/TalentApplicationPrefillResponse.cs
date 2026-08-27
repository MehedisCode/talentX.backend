using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;

public sealed record TalentApplicationPrefillResponse(
    string Name,
    string Email,
    string? Phone,
    string? Location,
    decimal? ExpectedSalary,
    decimal? TotalYearsExperience,
    string? ResumeStorageKey,
    string? ResumeOriginalFileName,
    string? ResumeUrl,
    IReadOnlyList<WorkExperienceItemDto> WorkExperiences
);

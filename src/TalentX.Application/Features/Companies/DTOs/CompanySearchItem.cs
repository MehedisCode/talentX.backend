namespace TalentX.Application.Features.Companies.DTOs;

/// <summary>
/// Flat, scalar-only company row with active-job aggregates.
/// Scalar-only so EF can translate filtering, ordering and paging
/// (entity-typed projections cannot be ordered by in LINQ-to-Entities).
/// The handler maps it to <see cref="CompanyListItemDto"/>.
/// </summary>
public sealed record CompanySearchItem(
    Guid Id,
    string EmployerId,
    string Name,
    string? LogoStorageKey,
    string? Description,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Location,
    string? LinkedInUrl,
    string? TwitterUrl,
    string? FacebookUrl,
    DateTime CreatedAt,
    int OpeningsCount,
    int VacanciesCount
);

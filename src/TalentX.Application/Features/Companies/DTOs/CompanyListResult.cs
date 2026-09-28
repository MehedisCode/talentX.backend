namespace TalentX.Application.Features.Companies.DTOs;

/// <summary>
/// Public company list response: one page of <see cref="CompanyListItemDto"/>
/// plus active-job aggregates over the whole filtered set.
/// </summary>
public sealed record CompanyListResult(
    IReadOnlyList<CompanyListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalOpenings,
    int TotalVacancies
);

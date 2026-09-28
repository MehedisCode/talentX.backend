namespace TalentX.Application.Features.Companies.DTOs;

/// <summary>
/// Company search page plus active-job aggregates over the whole filtered set.
/// Totals are independent of sorting and paging so they stay stable while browsing.
/// </summary>
public sealed record CompanySearchResult(
    IReadOnlyList<CompanySearchItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalOpenings,
    int TotalVacancies
);

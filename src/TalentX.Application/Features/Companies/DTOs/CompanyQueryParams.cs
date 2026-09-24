namespace TalentX.Application.Features.Companies.DTOs;

public sealed record CompanyQueryParams(
    string? Keyword = null,
    string? Industry = null,
    string? Location = null,
    string? CompanySize = null,
    string? SortBy = null,
    string? SortDir = null,
    int Page = 1,
    int PageSize = 10
);
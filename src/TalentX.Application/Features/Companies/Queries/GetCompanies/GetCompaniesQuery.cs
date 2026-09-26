using MediatR;
using TalentX.Application.Features.Companies.DTOs;

namespace TalentX.Application.Features.Companies.Queries.GetCompanies;

public sealed record GetCompaniesQuery(
    string? Keyword = null,
    string? Industry = null,
    string? Location = null,
    string? CompanySize = null,
    bool? Hiring = null,
    string? SortBy = null,
    string? SortDir = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<CompanyListResult>
{
    public GetCompaniesQuery(CompanyQueryParams parameters)
        : this(
            parameters.Keyword,
            parameters.Industry,
            parameters.Location,
            parameters.CompanySize,
            parameters.Hiring,
            parameters.SortBy,
            parameters.SortDir,
            parameters.Page,
            parameters.PageSize)
    {
    }
}

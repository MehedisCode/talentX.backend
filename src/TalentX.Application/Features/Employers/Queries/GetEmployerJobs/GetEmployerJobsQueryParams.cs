using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerJobs;

public sealed record GetEmployerJobsQueryParams(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = EmployerDashboardConstants.DefaultJobsPageSize,
    JobStatus? Status = null,
    bool? IsExpired = null,
    string? SearchTerm = null)
{
    public GetEmployerJobsQueryParams() : this(
        Page: PaginationRequest.DefaultPageNumber,
        PageSize: EmployerDashboardConstants.DefaultJobsPageSize,
        Status: null,
        IsExpired: null,
        SearchTerm: null)
    {
    }
}

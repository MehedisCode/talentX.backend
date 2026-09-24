using TalentX.Application.Common.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record AdminJobReportQueryParams
{
    public string? SearchQuery { get; init; }
    public JobReportStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

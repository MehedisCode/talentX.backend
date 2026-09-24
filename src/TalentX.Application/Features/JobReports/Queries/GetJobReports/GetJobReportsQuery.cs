using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReports;

public sealed record GetJobReportsQuery : IRequest<PaginatedList<JobReportListItemDto>>
{
    public string? SearchQuery { get; init; }
    public JobReportStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

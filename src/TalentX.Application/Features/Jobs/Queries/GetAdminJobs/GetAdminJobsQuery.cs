using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Queries.GetAdminJobs;

public sealed record GetAdminJobsQuery : IRequest<PaginatedList<AdminJobListItemDto>>
{
    public string? SearchQuery { get; init; }
    public JobStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

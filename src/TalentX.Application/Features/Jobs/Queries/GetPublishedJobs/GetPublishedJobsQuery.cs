using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

public sealed record GetPublishedJobsQuery(
    string? Keyword = null,
    string? Location = null,
    EmploymentType? EmploymentType = null,
    ExperienceLevel? ExperienceLevel = null,
    WorkArrangement? WorkArrangement = null,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<JobListItemDto>>
{
    public GetPublishedJobsQuery(GetJobsQueryParams parameters)
        : this(
            parameters.Keyword,
            parameters.Location,
            parameters.EmploymentType,
            parameters.ExperienceLevel,
            parameters.WorkArrangement,
            parameters.Page,
            parameters.PageSize)
    {
    }
}

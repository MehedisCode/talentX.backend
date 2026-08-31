using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

public record GetPublishedJobsQuery(
    string? Keyword = null,
    string? Location = null,
    EmploymentType? EmploymentType = null,
    ExperienceLevel? ExperienceLevel = null,
    WorkArrangement? WorkArrangement = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedList<JobListItemDto>>;

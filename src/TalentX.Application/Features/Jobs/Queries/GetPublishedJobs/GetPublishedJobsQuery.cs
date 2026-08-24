using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

public record GetPublishedJobsQuery(int Page, int PageSize) : IRequest<PaginatedList<JobListItemDto>>;

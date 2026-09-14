using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.Jobs.DTOs;

namespace TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;

public sealed record GetPreferredJobsQuery(PreferredJobsQueryParams Parameters) : IRequest<PaginatedList<JobListItemDto>>;

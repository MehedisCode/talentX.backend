using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerJobs;

public sealed record GetEmployerJobsQuery(
    int Page = 1,
    int PageSize = 10,
    JobStatus? Status = null,
    bool? IsExpired = null) : IRequest<PaginatedList<EmployerJobListItemDto>>;

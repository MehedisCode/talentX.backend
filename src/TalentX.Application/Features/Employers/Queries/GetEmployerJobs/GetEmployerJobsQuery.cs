using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerJobs;

public sealed record GetEmployerJobsQuery(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = EmployerDashboardConstants.DefaultJobsPageSize,
    JobStatus? Status = null,
    bool? IsExpired = null,
    string? SearchTerm = null) : IRequest<PaginatedList<EmployerJobListItemDto>>;

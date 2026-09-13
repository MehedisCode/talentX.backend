using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;

public sealed record GetJobApplicantsQuery(
    Guid JobId,
    string? Search = null,
    ApplicationStatus? Status = null,
    string? SortBy = null,
    string? SortOrder = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PaginatedList<JobApplicantListItemDto>>;

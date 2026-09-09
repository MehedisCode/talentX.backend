using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.SavedJobs.DTOs;

namespace TalentX.Application.Features.SavedJobs.Queries.GetSavedJobs;

public sealed record GetSavedJobsQuery(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<SavedJobResponse>>;

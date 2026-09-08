using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.DTOs;

namespace TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;

public sealed record GetEmployerJobInterviewsQuery(
    Guid JobId,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<EmployerInterviewDto>>;

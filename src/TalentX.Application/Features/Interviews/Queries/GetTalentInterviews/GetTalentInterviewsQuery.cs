using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.DTOs;

namespace TalentX.Application.Features.Interviews.Queries.GetTalentInterviews;

public sealed record GetTalentInterviewsQuery(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<TalentInterviewDto>>;

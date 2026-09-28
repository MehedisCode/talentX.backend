using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetMyCompanyReviews;

public sealed record GetMyCompanyReviewsQuery(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<MyCompanyReviewItemDto>>;

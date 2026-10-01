using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetPublicCompanyReviews;

public sealed record GetPublicCompanyReviewsQuery(
    Guid CompanyId,
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<PublicCompanyReviewDto>>;

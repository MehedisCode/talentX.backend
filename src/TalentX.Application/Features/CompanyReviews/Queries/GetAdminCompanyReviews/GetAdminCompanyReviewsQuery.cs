using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;

public sealed record GetAdminCompanyReviewsQuery : IRequest<PaginatedList<AdminCompanyReviewListItemDto>>
{
    public string? SearchQuery { get; init; }
    public CompanyReviewStatus? Status { get; init; }
    public ReviewSentiment? Sentiment { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;

public sealed class GetAdminCompanyReviewsQueryHandler
    : IRequestHandler<GetAdminCompanyReviewsQuery, PaginatedList<AdminCompanyReviewListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminCompanyReviewsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<AdminCompanyReviewListItemDto>> Handle(
        GetAdminCompanyReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var filter = new AdminCompanyReviewFilter
        {
            Status = request.Status,
            Sentiment = request.Sentiment,
            SearchQuery = request.SearchQuery
        };

        var reviews = await _unitOfWork.CompanyReviews.GetAdminPagedAsync(filter, page, pageSize, cancellationToken);

        var talentUserIds = reviews.Items.Select(item => item.TalentUserId).Distinct().ToList();
        var usersByUserId = (await _unitOfWork.Users.GetByUserIdsAsync(talentUserIds, cancellationToken))
            .ToDictionary(user => user.UserId);

        var items = reviews.Items.Select(item => WithReviewer(item, usersByUserId)).ToList();

        return new PaginatedList<AdminCompanyReviewListItemDto>(items, reviews.Page, reviews.PageSize, reviews.TotalCount);
    }

    private static AdminCompanyReviewListItemDto WithReviewer(
        AdminCompanyReviewListItemDto item,
        IReadOnlyDictionary<string, User> usersByUserId)
    {
        if (usersByUserId.TryGetValue(item.TalentUserId, out var reviewer))
        {
            return item with { ReviewerName = reviewer.Name, ReviewerEmail = reviewer.Email, ReviewerUserId = reviewer.Id };
        }

        return item with { ReviewerName = "Deleted user", ReviewerEmail = string.Empty, ReviewerUserId = null };
    }
}

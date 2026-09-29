using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;

public sealed class GetAdminCompanyReviewStatsQueryHandler
    : IRequestHandler<GetAdminCompanyReviewStatsQuery, AdminCompanyReviewStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminCompanyReviewStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminCompanyReviewStatsDto> Handle(GetAdminCompanyReviewStatsQuery request, CancellationToken cancellationToken)
    {
        var counts = await _unitOfWork.CompanyReviews.GetStatusCountsAsync(cancellationToken);

        return new AdminCompanyReviewStatsDto
        {
            Total = counts.Values.Sum(),
            PendingCount = counts.GetValueOrDefault(CompanyReviewStatus.Pending),
            PublishedCount = counts.GetValueOrDefault(CompanyReviewStatus.Published),
            RejectedCount = counts.GetValueOrDefault(CompanyReviewStatus.Rejected),
            HiddenCount = counts.GetValueOrDefault(CompanyReviewStatus.Hidden),
            WithdrawnCount = counts.GetValueOrDefault(CompanyReviewStatus.Withdrawn)
        };
    }
}

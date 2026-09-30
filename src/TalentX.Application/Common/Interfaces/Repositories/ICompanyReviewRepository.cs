using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface ICompanyReviewRepository
{
    Task AddAsync(CompanyReview review, CancellationToken cancellationToken = default);
    Task<CompanyReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyReview?> GetByJobApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyReview>> GetByJobApplicationIdsAsync(IEnumerable<Guid> jobApplicationIds, CancellationToken cancellationToken = default);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
    Task DeleteByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CompanyReviewSummaryDto> GetPublishedSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<PaginatedList<AdminCompanyReviewListItemDto>> GetAdminPagedAsync(
        AdminCompanyReviewFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<CompanyReviewStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    Task<PaginatedList<CompanyReview>> GetByCompanyIdPagedAsync(
        Guid companyId,
        CompanyReviewStatus? status,
        ReviewSentiment? sentiment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<CompanyReviewStatus, int>> GetStatusCountsByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;

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
}

using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class CompanyReviewRepository : ICompanyReviewRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CompanyReview review, CancellationToken cancellationToken = default)
    {
        await _context.CompanyReviews.AddAsync(review, cancellationToken);
    }

    // Tracked: handlers mutate the entity through its behaviour methods and save.
    public async Task<CompanyReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyReviews
            .FirstOrDefaultAsync(review => review.Id == id, cancellationToken);
    }

    public async Task<CompanyReview?> GetByJobApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyReviews
            .FirstOrDefaultAsync(review => review.JobApplicationId == jobApplicationId, cancellationToken);
    }

    // Batch lookup (one query) so callers listing many applications avoid N+1.
    public async Task<IReadOnlyList<CompanyReview>> GetByJobApplicationIdsAsync(IEnumerable<Guid> jobApplicationIds, CancellationToken cancellationToken = default)
    {
        var ids = jobApplicationIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<CompanyReview>();
        }

        return await _context.CompanyReviews
            .AsNoTracking()
            .Where(review => ids.Contains(review.JobApplicationId))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.CompanyReviews
            .Where(review => review.TalentUserId == talentUserId)
            .ToListAsync(cancellationToken);

        if (reviews.Count > 0)
        {
            _context.CompanyReviews.RemoveRange(reviews);
        }
    }

    public async Task DeleteByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.CompanyReviews
            .Where(review => review.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        if (reviews.Count > 0)
        {
            _context.CompanyReviews.RemoveRange(reviews);
        }
    }

    // One grouped query over Published reviews only. Averages are derived from sum/count
    // of the non-null ratings, and rounded in CompanyReviewSummaryCalculator alone.
    public async Task<CompanyReviewSummaryDto> GetPublishedSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var row = await _context.CompanyReviews
            .AsNoTracking()
            .Where(review => review.CompanyId == companyId && review.Status == CompanyReviewStatus.Published)
            .GroupBy(review => review.CompanyId)
            .Select(group => new
            {
                Total = group.Count(),
                Positive = group.Count(review => review.Sentiment == ReviewSentiment.Positive),
                RecruitmentCount = group.Count(review => review.RecruitmentProcessRating != null),
                RecruitmentSum = group.Sum(review => review.RecruitmentProcessRating ?? 0),
                CommunicationCount = group.Count(review => review.CommunicationRating != null),
                CommunicationSum = group.Sum(review => review.CommunicationRating ?? 0),
                InterviewCount = group.Count(review => review.InterviewExperienceRating != null),
                InterviewSum = group.Sum(review => review.InterviewExperienceRating ?? 0),
                ProfessionalismCount = group.Count(review => review.ProfessionalismRating != null),
                ProfessionalismSum = group.Sum(review => review.ProfessionalismRating ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return CompanyReviewSummaryCalculator.Empty;
        }

        return CompanyReviewSummaryCalculator.Create(new CompanyReviewAggregates(
            row.Total,
            row.Positive,
            new RatingAggregate(row.RecruitmentCount, row.RecruitmentSum),
            new RatingAggregate(row.CommunicationCount, row.CommunicationSum),
            new RatingAggregate(row.InterviewCount, row.InterviewSum),
            new RatingAggregate(row.ProfessionalismCount, row.ProfessionalismSum)));
    }
}

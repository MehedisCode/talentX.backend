using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
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

    public async Task<PaginatedList<AdminCompanyReviewListItemDto>> GetAdminPagedAsync(
        AdminCompanyReviewFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Inner join: CompanyId has an enforced FK (Cascade), so a review's company always exists.
        var query =
            from review in _context.CompanyReviews.AsNoTracking()
            join company in _context.Companies.AsNoTracking() on review.CompanyId equals company.Id
            select new { Review = review, Company = company };

        if (filter.Status.HasValue)
        {
            query = query.Where(row => row.Review.Status == filter.Status.Value);
        }

        if (filter.Sentiment.HasValue)
        {
            query = query.Where(row => row.Review.Sentiment == filter.Sentiment.Value);
        }

        if (filter.CompanyId.HasValue)
        {
            query = query.Where(row => row.Review.CompanyId == filter.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var normalizedSearchQuery = filter.SearchQuery.Trim().ToLower();
            query = query.Where(row =>
                row.Company.Name.ToLower().Contains(normalizedSearchQuery) ||
                row.Review.JobTitle.ToLower().Contains(normalizedSearchQuery));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedRows = await query
            // Pending reviews are the moderation queue, so they come first, oldest submitted
            // first (fair FIFO). Everything else is for reference only, newest first.
            .OrderBy(row => row.Review.Status == CompanyReviewStatus.Pending ? 0 : 1)
            .ThenBy(row => row.Review.Status == CompanyReviewStatus.Pending ? row.Review.SubmittedAt : DateTime.MaxValue)
            .ThenByDescending(row => row.Review.Status == CompanyReviewStatus.Pending ? DateTime.MinValue : row.Review.SubmittedAt)
            .ThenByDescending(row => row.Review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pagedRows.Select(row => new AdminCompanyReviewListItemDto
        {
            Id = row.Review.Id,
            Status = row.Review.Status,
            Sentiment = row.Review.Sentiment,
            Comment = row.Review.Comment,
            Ratings = new CompanyReviewRatingsDto(
                row.Review.RecruitmentProcessRating,
                row.Review.CommunicationRating,
                row.Review.InterviewExperienceRating,
                row.Review.ProfessionalismRating),
            CompanyId = row.Company.Id,
            CompanyName = row.Company.Name,
            JobTitle = row.Review.JobTitle,
            TalentUserId = row.Review.TalentUserId,
            SubmittedAt = row.Review.SubmittedAt,
            UpdatedAt = row.Review.UpdatedAt,
            PublishedAt = row.Review.PublishedAt,
            ModerationReason = row.Review.ModerationReason,
            ModerationDetails = row.Review.ModerationDetails,
            ModeratedAt = row.Review.ModeratedAt
        }).ToList();

        return new PaginatedList<AdminCompanyReviewListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyDictionary<CompanyReviewStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CompanyReviews
            .AsNoTracking()
            .GroupBy(review => review.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<PaginatedList<CompanyReview>> GetByCompanyIdPagedAsync(
        Guid companyId,
        CompanyReviewStatus? status,
        ReviewSentiment? sentiment,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CompanyReviews
            .AsNoTracking()
            .Where(review => review.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(review => review.Status == status.Value);
        }

        if (sentiment.HasValue)
        {
            query = query.Where(review => review.Sentiment == sentiment.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(review => review.SubmittedAt)
            .ThenByDescending(review => review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<CompanyReview>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyDictionary<CompanyReviewStatus, int>> GetStatusCountsByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyReviews
            .AsNoTracking()
            .Where(review => review.CompanyId == companyId)
            .GroupBy(review => review.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }
}

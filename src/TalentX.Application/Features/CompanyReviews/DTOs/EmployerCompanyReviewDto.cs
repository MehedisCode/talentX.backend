using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>
/// An employer's anonymized, read-only view of one review about their company. Deliberately
/// excludes reviewer identity, job/application ids, and moderation reason/details — an employer
/// must never be able to identify who wrote a review or why it was moderated.
/// </summary>
public sealed record EmployerCompanyReviewDto(
    Guid Id,
    CompanyReviewStatus Status,
    ReviewSentiment Sentiment,
    string? Comment,
    CompanyReviewRatingsDto Ratings,
    string JobTitle,
    DateTime SubmittedAt,
    DateTime? UpdatedAt);

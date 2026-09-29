using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>A talent's own review, including its moderation state.</summary>
public sealed record MyCompanyReviewDto(
    Guid Id,
    CompanyReviewStatus Status,
    ReviewSentiment Sentiment,
    string? Comment,
    CompanyReviewRatingsDto Ratings,
    ReviewModerationReason? ModerationReason,
    string? ModerationDetails,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt);

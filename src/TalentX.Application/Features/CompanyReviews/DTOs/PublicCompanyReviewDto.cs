using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>
/// The public, anonymous view of one published review shown on a company's public profile.
/// Deliberately excludes reviewer identity, job title, application id, status, and moderation
/// fields — nothing here may identify who wrote the review.
/// </summary>
public sealed record PublicCompanyReviewDto(
    Guid Id,
    ReviewSentiment Sentiment,
    string? Comment,
    int? RecruitmentProcessRating,
    int? CommunicationRating,
    int? InterviewExperienceRating,
    int? ProfessionalismRating,
    DateTime PublishedAt);

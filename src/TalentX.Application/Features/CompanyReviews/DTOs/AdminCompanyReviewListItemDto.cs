using System.Text.Json.Serialization;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.DTOs;

public sealed record AdminCompanyReviewListItemDto
{
    public Guid Id { get; init; }
    public CompanyReviewStatus Status { get; init; }
    public ReviewSentiment Sentiment { get; init; }
    public string? Comment { get; init; }
    public required CompanyReviewRatingsDto Ratings { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;

    public string ReviewerName { get; init; } = string.Empty;
    public string ReviewerEmail { get; init; } = string.Empty;
    public Guid? ReviewerUserId { get; init; }

    [JsonIgnore]
    public string TalentUserId { get; init; } = string.Empty;

    public DateTime SubmittedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public ReviewModerationReason? ModerationReason { get; init; }
    public string? ModerationDetails { get; init; }
    public DateTime? ModeratedAt { get; init; }
}

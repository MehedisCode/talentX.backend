namespace TalentX.Application.Features.CompanyReviews.DTOs;

public sealed record AdminCompanyReviewStatsDto
{
    public int Total { get; init; }
    public int PendingCount { get; init; }
    public int PublishedCount { get; init; }
    public int RejectedCount { get; init; }
    public int HiddenCount { get; init; }
    public int WithdrawnCount { get; init; }
}

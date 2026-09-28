using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews;

/// <summary>Raw counts the repository reads from the database for one rating area (nulls excluded).</summary>
public sealed record RatingAggregate(int Count, int Sum);

public sealed record CompanyReviewAggregates(
    int TotalCount,
    int PositiveCount,
    RatingAggregate RecruitmentProcess,
    RatingAggregate Communication,
    RatingAggregate InterviewExperience,
    RatingAggregate Professionalism);

/// <summary>
/// The only place summary numbers are rounded, so every endpoint that shows a
/// company's review summary reports identical values.
/// </summary>
public static class CompanyReviewSummaryCalculator
{
    public static CompanyReviewSummaryDto Empty { get; } = Create(new CompanyReviewAggregates(
        0,
        0,
        new RatingAggregate(0, 0),
        new RatingAggregate(0, 0),
        new RatingAggregate(0, 0),
        new RatingAggregate(0, 0)));

    public static CompanyReviewSummaryDto Create(CompanyReviewAggregates aggregates) => new(
        aggregates.TotalCount,
        aggregates.PositiveCount,
        aggregates.TotalCount - aggregates.PositiveCount,
        PositivePercent(aggregates.PositiveCount, aggregates.TotalCount),
        Area(aggregates.RecruitmentProcess),
        Area(aggregates.Communication),
        Area(aggregates.InterviewExperience),
        Area(aggregates.Professionalism));

    private static decimal PositivePercent(int positive, int total) =>
        total == 0 ? 0 : Math.Round(positive * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static RatingAreaSummaryDto Area(RatingAggregate aggregate) => new(
        aggregate.Count == 0
            ? null
            : Math.Round((decimal)aggregate.Sum / aggregate.Count, 1, MidpointRounding.AwayFromZero),
        aggregate.Count);
}

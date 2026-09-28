namespace TalentX.Application.Features.CompanyReviews.DTOs;

public sealed record RatingAreaSummaryDto(decimal? Average, int RatingCount);

public sealed record CompanyReviewSummaryDto(
    int TotalCount,
    int PositiveCount,
    int NegativeCount,
    decimal PositivePercent,
    RatingAreaSummaryDto RecruitmentProcess,
    RatingAreaSummaryDto Communication,
    RatingAreaSummaryDto InterviewExperience,
    RatingAreaSummaryDto Professionalism);

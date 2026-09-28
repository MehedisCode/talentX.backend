namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>The four optional 1–5 ratings of a review. A <c>null</c> rating means "not rated".</summary>
public sealed record CompanyReviewRatingsDto(
    int? RecruitmentProcess,
    int? Communication,
    int? InterviewExperience,
    int? Professionalism);

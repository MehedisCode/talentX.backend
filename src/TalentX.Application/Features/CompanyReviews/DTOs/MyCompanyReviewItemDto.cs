using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>
/// One row of the talent's Reviews tab: an application with an accepted interview, whether the
/// review can be written yet, and the review itself once it exists.
/// </summary>
/// <param name="CompanyId">Null only while the company's profile row hasn't been created yet.</param>
/// <param name="ReviewUnlocksAt">When submitting unlocks; null if it can't be derived from the interview.</param>
public sealed record MyCompanyReviewItemDto(
    Guid JobApplicationId,
    string JobTitle,
    Guid? CompanyId,
    string CompanyName,
    InterviewType InterviewType,
    DateTime? ReviewUnlocksAt,
    bool CanSubmit,
    MyCompanyReviewDto? Review,
    bool CanEdit,
    bool CanWithdraw);

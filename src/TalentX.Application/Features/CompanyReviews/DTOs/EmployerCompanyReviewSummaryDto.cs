namespace TalentX.Application.Features.CompanyReviews.DTOs;

/// <summary>The employer's summary view: the same published-only summary the public sees, plus a
/// count of reviews per status so the employer understands what is still awaiting moderation.</summary>
public sealed record EmployerCompanyReviewSummaryDto(
    CompanyReviewSummaryDto Summary,
    IReadOnlyDictionary<string, int> CountsByStatus);

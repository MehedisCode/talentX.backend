using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.Companies.DTOs;

public sealed record PublicCompanyProfileDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    string? Description,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Location,
    string? LinkedIn,
    string? Twitter,
    string? Facebook,
    int ActiveJobsCount,
    IReadOnlyList<PublicCompanyJobDto> PublishedJobs,
    CompanyReviewSummaryDto ReviewSummary
);

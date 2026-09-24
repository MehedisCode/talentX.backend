using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.CompanyReviews;

public sealed class CompanyReviewSettings : ICompanyReviewSettings
{
    public const int DefaultAssessmentReviewDelayDays = 1;

    public int AssessmentReviewDelayDays { get; init; } = DefaultAssessmentReviewDelayDays;

    // Read from configuration only (appsettings, CompanyReview:AssessmentReviewDelayDays) — there is
    // deliberately no dedicated environment variable. An unparseable value falls back to the default;
    // a negative one is clamped to 0.
    public static CompanyReviewSettings FromConfiguration(IConfiguration configuration)
    {
        var raw = configuration["CompanyReview:AssessmentReviewDelayDays"];

        var days = int.TryParse(raw, out var parsed) ? parsed : DefaultAssessmentReviewDelayDays;

        return new CompanyReviewSettings { AssessmentReviewDelayDays = Math.Max(0, days) };
    }
}

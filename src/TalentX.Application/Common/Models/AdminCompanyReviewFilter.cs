using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Models;

public sealed record AdminCompanyReviewFilter
{
    public CompanyReviewStatus? Status { get; init; }
    public ReviewSentiment? Sentiment { get; init; }
    public string? SearchQuery { get; init; }
    public Guid? CompanyId { get; init; }
}

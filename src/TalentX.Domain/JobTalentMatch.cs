using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class JobTalentMatch : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public decimal Score { get; private set; }
    public string? CriteriaSnapshot { get; private set; }
    public DateTime MatchedAt { get; private set; }

    private JobTalentMatch() { }

    public JobTalentMatch(Guid jobId, string talentUserId, decimal score, string? criteriaSnapshot = null)
    {
        JobId = jobId;
        TalentUserId = talentUserId;
        Score = score;
        CriteriaSnapshot = criteriaSnapshot;
        MatchedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateScore(decimal score, string? criteriaSnapshot = null)
    {
        Score = score;
        CriteriaSnapshot = criteriaSnapshot;
        MatchedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

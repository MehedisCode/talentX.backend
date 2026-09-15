using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobTalentMatch : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public decimal Score { get; private set; }
    public string? CriteriaSnapshot { get; private set; }
    public DateTime MatchedAt { get; private set; }
    public MatchStatus Status { get; private set; }
    public string? SkillBreakdown { get; private set; }
    public DateTime? LastUpdatedAt { get; private set; }

    private JobTalentMatch() { }

    public JobTalentMatch(Guid jobId, string talentUserId, decimal score, string? criteriaSnapshot = null)
    {
        JobId = jobId;
        TalentUserId = talentUserId;
        Score = score;
        CriteriaSnapshot = criteriaSnapshot;
        MatchedAt = DateTime.UtcNow;
        Status = MatchStatus.Active;
        SkillBreakdown = null;
        LastUpdatedAt = null;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateScore(decimal score, string? criteriaSnapshot = null)
    {
        Score = score;
        CriteriaSnapshot = criteriaSnapshot;
        MatchedAt = DateTime.UtcNow;
        Status = MatchStatus.Active;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = MatchStatus.Archived;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = MatchStatus.Expired;
        LastUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
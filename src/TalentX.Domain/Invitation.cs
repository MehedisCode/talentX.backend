using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class Invitation : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public DateTime InvitedDate { get; private set; }
    public bool IsActive { get; private set; }

    private Invitation() { }

    public Invitation(Guid jobId, string talentUserId)
    {
        JobId = jobId;
        TalentUserId = talentUserId;
        InvitedDate = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

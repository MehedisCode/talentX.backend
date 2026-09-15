using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class Invitation : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public DateTime InvitedDate { get; private set; }
    public InvitationStatus Status { get; private set; }
    public string? Message { get; private set; }
    public string? Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int InvitationCount { get; private set; }

    public bool IsActive { get; private set; } = true;

    private void SyncIsActive()
    {
        IsActive = Status == InvitationStatus.Pending || Status == InvitationStatus.Accepted;
    }

    private Invitation() { }

    public Invitation(Guid jobId, string talentUserId, DateTime jobExpireAt)
    {
        JobId = jobId;
        TalentUserId = talentUserId;
        InvitedDate = DateTime.UtcNow;
        Status = InvitationStatus.Pending;
        Message = null;
        Token = Guid.NewGuid().ToString("N");
        ExpiresAt = jobExpireAt;
        InvitationCount = 1;
        CreatedAt = DateTime.UtcNow;
        SyncIsActive();
    }

    public void Accept()
    {
        Status = InvitationStatus.Accepted;
        SyncIsActive();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Decline()
    {
        Status = InvitationStatus.Declined;
        SyncIsActive();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = InvitationStatus.Expired;
        SyncIsActive();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw()
    {
        Status = InvitationStatus.Withdrawn;
        SyncIsActive();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = InvitationStatus.Withdrawn;
        SyncIsActive();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAttempt()
    {
        InvitationCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}

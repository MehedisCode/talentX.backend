using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class Invitation : BaseEntity
{
    public Guid JobId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public DateTime InvitedDate { get; private set; }
    public bool IsActive { get; private set; }
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public string? Token { get; private set; }
    public string? Message { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int InvitationCount { get; private set; } = 1;

    private Invitation() { }

    public Invitation(Guid jobId, string talentAccountId, string? message = null, TimeSpan? lifetime = null)
    {
        JobId = jobId;
        TalentUserId = talentAccountId;
        InvitedDate = DateTime.UtcNow;
        IsActive = true;
        Status = InvitationStatus.Pending;
        Token = GenerateToken();
        Message = message;
        ExpiresAt = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(14));
        InvitationCount = 1;
        CreatedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        Status = InvitationStatus.Accepted;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Decline()
    {
        Status = InvitationStatus.Declined;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw()
    {
        Status = InvitationStatus.Withdrawn;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}

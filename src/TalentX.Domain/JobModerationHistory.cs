using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobModerationHistory : BaseEntity
{
    public Guid JobId { get; private set; }
    public JobModerationAction Action { get; private set; }
    public JobStatus FromStatus { get; private set; }
    public JobStatus ToStatus { get; private set; }
    public DeactivationReason? Reason { get; private set; }
    public string? Details { get; private set; }
    public string ActorUserId { get; private set; } = string.Empty;
    public string ActorEmail { get; private set; } = string.Empty;

    private JobModerationHistory()
    {
    }

    public static JobModerationHistory Create(
        Guid jobId,
        JobModerationAction action,
        JobStatus fromStatus,
        JobStatus toStatus,
        string actorUserId,
        string actorEmail,
        DeactivationReason? reason = null,
        string? details = null)
    {
        return new JobModerationHistory
        {
            JobId = jobId,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Reason = reason,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
    }
}

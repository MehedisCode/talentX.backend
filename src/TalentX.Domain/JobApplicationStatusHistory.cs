using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobApplicationStatusHistory : BaseEntity
{
    public Guid JobApplicationId { get; private set; }
    public ApplicationStatus? FromStatus { get; private set; }
    public ApplicationStatus ToStatus { get; private set; }
    public string ChangedByUserId { get; private set; } = string.Empty;
    public DateTime ChangedAt { get; private set; }

    private JobApplicationStatusHistory()
    {
    }

    public static JobApplicationStatusHistory Create(
        Guid jobApplicationId,
        ApplicationStatus? fromStatus,
        ApplicationStatus toStatus,
        string changedByUserId)
    {
        return new JobApplicationStatusHistory
        {
            JobApplicationId = jobApplicationId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

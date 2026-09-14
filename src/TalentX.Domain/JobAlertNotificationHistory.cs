using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class JobAlertNotificationHistory : BaseEntity
{
    public string TalentId { get; private set; } = string.Empty;
    public Guid JobId { get; private set; }
    public Guid JobAlertId { get; private set; }
    public DateTime SentAt { get; private set; }

    private JobAlertNotificationHistory()
    {
    }

    public static JobAlertNotificationHistory Create(string talentId, Guid jobId, Guid jobAlertId)
    {
        return new JobAlertNotificationHistory
        {
            TalentId = talentId,
            JobId = jobId,
            JobAlertId = jobAlertId,
            SentAt = DateTime.UtcNow
        };
    }
}

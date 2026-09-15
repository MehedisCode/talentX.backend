using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class TalentNotificationPreference : BaseEntity
{
    public string TalentId { get; private set; } = string.Empty;
    public bool JobAlertsEmailEnabled { get; private set; }

    private TalentNotificationPreference()
    {
    }

    public static TalentNotificationPreference CreateDefault(string talentId)
    {
        return new TalentNotificationPreference
        {
            TalentId = talentId,
            JobAlertsEmailEnabled = true
        };
    }

    public void Update(bool jobAlertsEmailEnabled)
    {
        JobAlertsEmailEnabled = jobAlertsEmailEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}

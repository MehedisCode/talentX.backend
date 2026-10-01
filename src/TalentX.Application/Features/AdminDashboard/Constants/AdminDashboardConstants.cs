namespace TalentX.Application.Features.AdminDashboard.Constants;

public static class AdminDashboardConstants
{
    public const int DefaultRangeDays = 30;
    public const int MaxRangeDays = 366;

    // How many recent items to load per type before they are merged and trimmed to RecentActivityLimit.
    public const int RecentActivityPerType = 3;
    public const int RecentActivityLimit = 6;
}

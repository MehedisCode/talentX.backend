namespace TalentX.Application.Features.InterviewPreparation;

/// <summary>
/// Configuration for the interview preparation feature, bound from the
/// "InterviewPreparationSettings" section of appsettings.json so limits and
/// windows can change between environments without code changes.
/// </summary>
public sealed class InterviewPreparationSettings
{
    public const string SectionName = "InterviewPreparationSettings";

    /// <summary>Max new preparations a talent may generate per day.</summary>
    public int DailyGenerateLimit { get; set; } = 5;

    /// <summary>Max preparation generations (create + regenerate) per day.</summary>
    public int DailyGenerationLimit { get; set; } = 10;

    /// <summary>Max times a single preparation may be regenerated.</summary>
    public int MaxRegenerationCount { get; set; } = 3;

    /// <summary>Minutes after the scheduled interview time that a preparation stays accessible.</summary>
    public int AccessWindowAfterInterviewMinutes { get; set; } = 30;

    /// <summary>Rate-limit permits for the generation endpoints within the window.</summary>
    public int RateLimitPermits { get; set; } = 10;

    /// <summary>Length of the rate-limit window, in hours.</summary>
    public int RateLimitWindowHours { get; set; } = 24;

    /// <summary>The configured access window as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan AccessWindowAfterInterview => TimeSpan.FromMinutes(AccessWindowAfterInterviewMinutes);
}

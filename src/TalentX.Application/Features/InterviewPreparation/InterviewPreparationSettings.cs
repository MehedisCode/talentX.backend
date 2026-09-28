namespace TalentX.Application.Features.InterviewPreparation;

public sealed class InterviewPreparationSettings
{
    public const string SectionName = "InterviewPreparationSettings";

    public int DailyGenerateLimit { get; set; } = 5;

    public int DailyGenerationLimit { get; set; } = 10;

    public int MaxRegenerationCount { get; set; } = 3;

    public int AccessWindowAfterInterviewMinutes { get; set; } = 30;

    public int RateLimitPermits { get; set; } = 10;

    public int RateLimitWindowHours { get; set; } = 24;

    public TimeSpan AccessWindowAfterInterview => TimeSpan.FromMinutes(AccessWindowAfterInterviewMinutes);
}

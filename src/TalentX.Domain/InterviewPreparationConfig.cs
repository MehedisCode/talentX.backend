using TalentX.Domain.Common;

namespace TalentX.Domain;

public class InterviewPreparationConfig : BaseEntity
{
    public int DailyGenerateLimit { get; private set; }
    public int DailyGenerationLimit { get; private set; }
    public int MaxRegenerationCount { get; private set; }
    public int AccessWindowAfterInterviewMinutes { get; private set; }

    private InterviewPreparationConfig() { }

    public static InterviewPreparationConfig CreateDefault(
        int dailyGenerateLimit = 5,
        int dailyGenerationLimit = 10,
        int maxRegenerationCount = 3,
        int accessWindowAfterInterviewMinutes = 30)
    {
        return new InterviewPreparationConfig
        {
            DailyGenerateLimit = dailyGenerateLimit,
            DailyGenerationLimit = dailyGenerationLimit,
            MaxRegenerationCount = maxRegenerationCount,
            AccessWindowAfterInterviewMinutes = accessWindowAfterInterviewMinutes
        };
    }

    public void Update(
        int dailyGenerateLimit,
        int dailyGenerationLimit,
        int maxRegenerationCount,
        int accessWindowAfterInterviewMinutes)
    {
        DailyGenerateLimit = dailyGenerateLimit;
        DailyGenerationLimit = dailyGenerationLimit;
        MaxRegenerationCount = maxRegenerationCount;
        AccessWindowAfterInterviewMinutes = accessWindowAfterInterviewMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public TimeSpan AccessWindowAfterInterview =>
        TimeSpan.FromMinutes(AccessWindowAfterInterviewMinutes);
}

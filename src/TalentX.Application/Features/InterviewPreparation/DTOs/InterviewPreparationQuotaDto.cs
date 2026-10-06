namespace TalentX.Application.Features.InterviewPreparation.DTOs;

// Remaining daily AI-generation budget for the current talent, so the UI can
// warn before the user hits a 429. "Generates" = initial preparations only
// (DailyGenerateLimit); "Generations" = every AI generation call today
// (DailyGenerationLimit).
public sealed record InterviewPreparationQuotaDto
{
    public int DailyGenerateLimit { get; init; }
    public int DailyGenerationLimit { get; init; }
    public int MaxRegenerationCount { get; init; }
    public int AccessWindowAfterInterviewMinutes { get; init; }

    public int GeneratesUsedToday { get; init; }
    public int GenerationsUsedToday { get; init; }

    public int RemainingGeneratesToday =>
        Math.Max(0, DailyGenerateLimit - GeneratesUsedToday);
    public int RemainingGenerationsToday =>
        Math.Max(0, DailyGenerationLimit - GenerationsUsedToday);
}

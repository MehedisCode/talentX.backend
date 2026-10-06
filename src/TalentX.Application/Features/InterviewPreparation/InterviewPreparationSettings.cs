namespace TalentX.Application.Features.InterviewPreparation;

public sealed class InterviewPreparationSettings
{
    public const string SectionName = "InterviewPreparationSettings";

    // Input token budget for the answer block of an evaluation call: the pool is
    // split evenly across answered questions so a long session cannot blow the
    // model's context window (≈4 chars per token).
    public const int DefaultEvaluationInputTokenBudget = 24000;
    public const int DefaultEvaluationPerAnswerTokenBudget = 1000;

    public int DailyGenerateLimit { get; set; } = 5;

    public int DailyGenerationLimit { get; set; } = 10;

    public int MaxRegenerationCount { get; set; } = 3;

    public int AccessWindowAfterInterviewMinutes { get; set; } = 30;

    public int RateLimitPermits { get; set; } = 10;

    public int RateLimitWindowHours { get; set; } = 24;

    public int EvaluateRateLimitPermits { get; set; } = 15;

    public int EvaluationInputTokenBudget { get; set; } = DefaultEvaluationInputTokenBudget;

    public int EvaluationPerAnswerTokenBudget { get; set; } = DefaultEvaluationPerAnswerTokenBudget;

    public TimeSpan AccessWindowAfterInterview => TimeSpan.FromMinutes(AccessWindowAfterInterviewMinutes);
}

namespace TalentX.Infrastructure.Ai.Constants;

public static class GeminiConstants
{
    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";
    public const string DefaultModelName = "gemini-3.7-flash";

    public const double EvaluationTemperature = 0.3;
    public const int EvaluationMinOutputTokens = 8192;
    public const int DefaultMaxOutputTokens = 8192;
}

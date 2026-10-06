namespace TalentX.Infrastructure.Ai.Models;

public sealed class GeminiUsageMetadata
{
    public int? PromptTokenCount { get; set; }
    public int? CandidatesTokenCount { get; set; }
    public int? TotalTokenCount { get; set; }
}

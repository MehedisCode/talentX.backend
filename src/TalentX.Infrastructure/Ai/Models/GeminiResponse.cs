namespace TalentX.Infrastructure.Ai.Models;

public sealed class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }

    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

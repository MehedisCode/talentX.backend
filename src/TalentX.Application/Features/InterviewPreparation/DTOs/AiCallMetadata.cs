namespace TalentX.Application.Features.InterviewPreparation.DTOs;

// Observability data captured from a single AI service call, persisted with
// the result so evaluation quality can be correlated with model/prompt later.
public sealed record AiCallMetadata
{
    public string ModelName { get; init; } = string.Empty;
    public string PromptVersion { get; init; } = string.Empty;
    public long LatencyMs { get; init; }
    public int InputTokenCount { get; init; }
    public int OutputTokenCount { get; init; }
}

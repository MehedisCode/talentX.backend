using TalentX.Application.Common.Models;
using TalentX.Infrastructure.Ai.Constants;

namespace TalentX.Infrastructure.Ai.Helpers;

public sealed record GeminiPromptBuildOptions(
    string? DeterministicEmail = null,
    string? DeterministicPhone = null,
    int MaxOutputTokens = GeminiConstants.DefaultMaxOutputTokens,
    ResumeExtractionPurpose Purpose = ResumeExtractionPurpose.JobApplication
);

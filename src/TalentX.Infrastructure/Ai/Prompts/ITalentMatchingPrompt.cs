using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Infrastructure.Ai.Prompts;

public interface ITalentMatchingPrompt
{
    string Version { get; }
    string BuildPrompt(TalentMatchingRequest request);
}

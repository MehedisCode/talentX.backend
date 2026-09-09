namespace TalentX.Infrastructure.Ai.Prompts;

public interface ITalentMatchingPromptFactory
{
    ITalentMatchingPrompt GetPrompt(string? version = null);
}

public sealed class TalentMatchingPromptFactory : ITalentMatchingPromptFactory
{
    private readonly IEnumerable<ITalentMatchingPrompt> _prompts;

    public TalentMatchingPromptFactory(IEnumerable<ITalentMatchingPrompt> prompts)
    {
        _prompts = prompts;
    }

    public ITalentMatchingPrompt GetPrompt(string? version = null)
    {
        var targetVersion = string.IsNullOrWhiteSpace(version) ? "v1" : version.Trim().ToLowerInvariant();
        return _prompts.FirstOrDefault(p => p.Version.Equals(targetVersion, StringComparison.OrdinalIgnoreCase))
            ?? _prompts.First(p => p.Version == "v1");
    }
}

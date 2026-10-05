namespace TalentX.Application.Features.AiConfiguration.Constants;

public static class AiConfigurationConstants
{
    public const int MaxModelNameLength = 100;
    public const string DataProtectionPurpose = "TalentX.AiConfiguration.ApiKey";
    public const string CacheKey = "ActiveAiConfig";
    public const string DefaultModelName = "gemini-3.7-flash";
    public const string MaskedKeyPlaceholder = "••••••••";
    public const string EnvironmentKeyMasked = "••••••••(env)";
    public const string NotConfigured = "Not configured";
}

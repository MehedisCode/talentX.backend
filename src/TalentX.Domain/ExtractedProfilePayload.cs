namespace TalentX.Domain;

public sealed class ExtractedProfilePayload
{
    public string? Tagline { get; init; }
    public string? ShortBio { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public List<string>? PortfolioUrls { get; init; }
    public List<string>? SkillPropositions { get; init; }
}

namespace TalentX.Infrastructure.Ai.Models;

public sealed class GeminiStructuredResult
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public List<string>? PortfolioUrls { get; set; }
    public List<string>? TechnicalSkills { get; set; }
    public List<string>? SoftSkills { get; set; }
    public List<string>? ToolsAndPlatforms { get; set; }
    public List<GeminiWorkExperience>? WorkExperience { get; set; }
    public List<GeminiEducation>? Education { get; set; }
}

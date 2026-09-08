namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class TalentProfileForMatching
{
    public string TalentUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public List<string> TechnicalSkills { get; set; } = new();
    public List<string> SoftSkills { get; set; } = new();
    public List<string> ToolsAndPlatforms { get; set; } = new();
    public decimal? TotalYearsExperience { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? Location { get; set; }
    public List<string> SkillPropositions { get; set; } = new();
    public List<WorkExperienceSummary> WorkExperiences { get; set; } = new();
    public List<EducationSummary> Educations { get; set; } = new();
}

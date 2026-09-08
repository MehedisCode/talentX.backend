namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class CandidateProfileSummary
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Tagline { get; set; }
    public string? ShortBio { get; set; }
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public List<string> AdditionalUrls { get; set; } = new();
    public decimal? TotalYearsExperience { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public List<string> SkillPropositions { get; set; } = new();
    public List<string> TechnicalSkills { get; set; } = new();
    public List<string> SoftSkills { get; set; } = new();
    public List<string> ToolsAndPlatforms { get; set; } = new();
    public string? ProfilePictureUrl { get; set; }
    public List<WorkExperienceSummary> WorkExperiences { get; set; } = new();
    public List<EducationSummary> Educations { get; set; } = new();
}

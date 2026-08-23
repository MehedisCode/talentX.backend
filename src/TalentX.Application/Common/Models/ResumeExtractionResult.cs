namespace TalentX.Application.Common.Models;

public sealed class ResumeExtractionResult
{
    public Guid ResumeUploadId { get; set; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? LinkedInUrl { get; init; }
    public List<string> PortfolioUrls { get; init; } = [];
    public List<WorkExperienceDto> WorkExperience { get; init; } = [];
    public List<EducationDto> Education { get; init; } = [];
    public List<string> TechnicalSkills { get; init; } = [];
    public List<string> SoftSkills { get; init; } = [];
    public List<string> ToolsAndPlatforms { get; init; } = [];
    public string FileStorageKey { get; set; } = string.Empty;
}

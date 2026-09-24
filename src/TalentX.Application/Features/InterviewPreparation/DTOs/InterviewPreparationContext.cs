namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record InterviewPreparationContext
{
    // Job information
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string? JobDescription { get; init; }
    public string? Responsibilities { get; init; }
    public string? Requirements { get; init; }
    public List<string> RequiredSkills { get; init; } = new();
    public string? ExperienceLevel { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkArrangement { get; init; }

    // Talent profile
    public string TalentName { get; init; } = string.Empty;
    public string? Tagline { get; init; }
    public string? ShortBio { get; init; }
    public decimal? TotalYearsExperience { get; init; }
    public List<string> TechnicalSkills { get; init; } = new();
    public List<string> SoftSkills { get; init; } = new();
    public List<string> ToolsAndPlatforms { get; init; } = new();
    public List<WorkExperienceSnapshot> WorkExperiences { get; init; } = new();
    public List<EducationSnapshot> Educations { get; init; } = new();

    // Interview context
    public string InterviewType { get; init; } = string.Empty;
    public DateTime? ScheduledAt { get; init; }
}

public sealed record WorkExperienceSnapshot(
    string CompanyName, string JobTitle,
    string StartDate, string? EndDate, bool IsCurrent);

public sealed record EducationSnapshot(
    string Institution, string Degree,
    string StartDate, string? EndDate);

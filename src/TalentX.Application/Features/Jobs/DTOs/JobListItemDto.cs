using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public class JobListItemDto
{
    public Guid Id { get; set; }
    public string EmployerId {get; set;} = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string WorkArrangement { get; set; } = string.Empty;
    public IReadOnlyList<string> RequiredSkills { get; set; } = new List<string>();

    /// <summary>
    /// When this job went live (first published, or most recently reopened).
    /// Falls back to the record's creation time for jobs published before
    /// PublishedAt existed. This DTO is only ever returned for published
    /// jobs, so "created" and "went live" are effectively the same concept
    /// here — this is intentionally not the Job's raw creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public SalaryType? SalaryType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Currency? SalaryCurrency { get; set; }
    public SalaryPeriod? SalaryPeriod { get; set; }
}

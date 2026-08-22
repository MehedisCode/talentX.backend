using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

/// <summary>
/// Domain model for Job posting.
/// </summary>
public class Job : BaseEntity
{
    public string EmployerId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public EmploymentType EmploymentType { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public WorkMode WorkMode { get; private set; }
    public ExperienceLevel? ExperienceLevel { get; private set; }
    public List<string> RequiredSkills { get; private set; } = new();
    public JobStatus Status { get; private set; } = JobStatus.Draft;
    public int Vacancies { get; private set; }
    public DateTime ExpireAt { get; private set; }

    protected Job() { }

    public Job(
        string title,
        string companyName,
        string description,
        EmploymentType employmentType,
        string location,
        WorkMode workMode,
        ExperienceLevel? experienceLevel,
        List<string>? requiredSkills,
        int vacancies,
        DateTime expireAt,
        string employerId = "",
        JobStatus status = JobStatus.Draft
    )
    {
        Title = title;
        CompanyName = companyName;
        Description = description;
        EmploymentType = employmentType;
        Location = location;
        WorkMode = workMode;
        ExperienceLevel = experienceLevel;
        RequiredSkills = requiredSkills ?? new List<string>();
        Vacancies = vacancies;
        ExpireAt = expireAt;
        EmployerId = employerId;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string title,
        string companyName,
        string description,
        EmploymentType employmentType,
        string location,
        WorkMode workMode,
        ExperienceLevel? experienceLevel,
        List<string>? requiredSkills,
        int vacancies,
        DateTime expireAt
    )
    {
        Title = title;
        CompanyName = companyName;
        Description = description;
        EmploymentType = employmentType;
        Location = location;
        WorkMode = workMode;
        ExperienceLevel = experienceLevel;
        RequiredSkills = requiredSkills ?? new List<string>();
        Vacancies = vacancies;
        ExpireAt = expireAt;
        UpdatedAt = DateTime.UtcNow;
    }
}

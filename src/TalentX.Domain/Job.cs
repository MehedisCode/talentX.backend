using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public class Job : BaseEntity
{
    public string EmployerId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public EmploymentType EmploymentType { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public WorkArrangement WorkArrangement { get; private set; }
    public ExperienceLevel ExperienceLevel { get; private set; }
    public List<string> RequiredSkills { get; private set; } = new();
    public JobStatus Status { get; private set; }
    public int Vacancies { get; private set; }
    public DateTime ExpireAt { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public short? MinOverlapHours { get; private set; }
    public string? Responsibilities { get; private set; }

    private Job() { }

    public static Job Create(
        string employerId,
        string title,
        string companyName,
        string description,
        EmploymentType employmentType,
        ExperienceLevel experienceLevel,
        string location,
        WorkArrangement workArrangement,
        List<string> requiredSkills,
        int vacancies,
        DateTime expireAt
    )
    {
        return new Job
        {
            EmployerId = employerId,
            Title = title,
            CompanyName = companyName,
            Description = description,
            EmploymentType = employmentType,
            ExperienceLevel = experienceLevel,
            Location = location,
            WorkArrangement = workArrangement,
            RequiredSkills = requiredSkills,
            Vacancies = vacancies,
            ExpireAt = expireAt,
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetDetails(
        string title,
        string companyName,
        string description,
        EmploymentType employmentType,
        ExperienceLevel experienceLevel,
        string location,
        WorkArrangement workArrangement,
        List<string> requiredSkills,
        int vacancies,
        DateTime expireAt
    )
    {
        Title = title;
        CompanyName = companyName;
        Description = description;
        EmploymentType = employmentType;
        ExperienceLevel = experienceLevel;
        Location = location;
        WorkArrangement = workArrangement;
        RequiredSkills = requiredSkills;
        Vacancies = vacancies;
        ExpireAt = expireAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateWizardDetails(
        string? city,
        string? country,
        short? minOverlapHours,
        string? responsibilities)
    {
        City = city;
        Country = country;
        MinOverlapHours = minOverlapHours;
        Responsibilities = responsibilities;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPublished()
    {
        Status = JobStatus.Published;
    }
}

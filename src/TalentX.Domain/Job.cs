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
    public string? Requirements { get; private set; }
    public SalaryType? SalaryType { get; private set; }
    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public Currency? SalaryCurrency { get; private set; }
    public SalaryPeriod? SalaryPeriod { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DeactivationReason? DeactivationReason { get; private set; }
    public string? DeactivationDetails { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

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
        string? responsibilities,
        SalaryType? salaryType = null,
        decimal? salaryMin = null,
        decimal? salaryMax = null,
        Currency? salaryCurrency = null,
        SalaryPeriod? salaryPeriod = null,
        string? requirements = null)
    {
        City = city;
        Country = country;
        MinOverlapHours = minOverlapHours;
        Responsibilities = responsibilities;
        Requirements = requirements;
        SalaryType = salaryType;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryCurrency = salaryCurrency;
        SalaryPeriod = salaryPeriod;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPublished()
    {
        Status = JobStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void MarkAsClosed()
    {
        // Without this guard an employer could Close then Reopen a Deactivated job,
        // putting it back online without admin approval.
        if (Status != JobStatus.Published)
        {
            throw new InvalidOperationException("Only published jobs can be closed.");
        }

        Status = JobStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        if (Status != JobStatus.Closed)
        {
            throw new InvalidOperationException("Only closed jobs can be reopened.");
        }

        if (ExpireAt < DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("Cannot reopen a job with an expired deadline. Update the application deadline before reopening.");
        }

        Status = JobStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(DeactivationReason reason, string details)
    {
        if (Status is not (JobStatus.Published or JobStatus.PendingReview))
        {
            throw new InvalidOperationException("Only published or under-review jobs can be deactivated.");
        }

        Status = JobStatus.Deactivated;
        DeactivationReason = reason;
        DeactivationDetails = details;
        DeactivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitForReview()
    {
        if (Status != JobStatus.Deactivated)
        {
            throw new InvalidOperationException("Only deactivated jobs can be resubmitted for review.");
        }

        Status = JobStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status is not (JobStatus.Deactivated or JobStatus.PendingReview))
        {
            throw new InvalidOperationException("Only deactivated or under-review jobs can be reactivated.");
        }

        if (ExpireAt < DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException("Cannot reactivate a job with an expired deadline. The employer must update the application deadline first.");
        }

        // The reason only describes why the job is currently down; the full record
        // stays in JobModerationHistory.
        Status = JobStatus.Published;
        DeactivationReason = null;
        DeactivationDetails = null;
        DeactivatedAt = null;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

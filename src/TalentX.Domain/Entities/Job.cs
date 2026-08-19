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


    private Job() { }

    public static Job CreateDraft(
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
        var job = new Job
        {
            EmployerId = employerId.Trim(),
            Title = title.Trim(),
            CompanyName = companyName.Trim(),
            Description = description.Trim(),
            EmploymentType = employmentType,
            ExperienceLevel = experienceLevel,
            Location = location.Trim(),
            WorkArrangement = workArrangement,
            RequiredSkills = requiredSkills?.Where(s => !string.IsNullOrWhiteSpace(s))
                                             .Select(s => s.Trim())
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList() ?? new List<string>(),
            Vacancies = vacancies,
            ExpireAt = expireAt,
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        return job;
    }

    // Partial update: a null argument leaves the corresponding field unchanged.
    public void UpdateDetails(
        string? title,
        string? companyName,
        string? description,
        EmploymentType? employmentType,
        ExperienceLevel? experienceLevel,
        string? location,
        WorkArrangement? workArrangement,
        List<string>? requiredSkills,
        int? vacancies,
        DateTime? expireAt
    )
    {
        if (title != null) Title = title.Trim();
        if (companyName != null) CompanyName = companyName.Trim();
        if (description != null) Description = description.Trim();
        if (employmentType.HasValue) EmploymentType = employmentType.Value;
        if (experienceLevel.HasValue) ExperienceLevel = experienceLevel.Value;
        if (location != null) Location = location.Trim();
        if (workArrangement.HasValue) WorkArrangement = workArrangement.Value;
        if (requiredSkills != null)
        {
            RequiredSkills = requiredSkills.Where(s => !string.IsNullOrWhiteSpace(s))
                                            .Select(s => s.Trim())
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToList();
        }
        if (vacancies.HasValue) Vacancies = vacancies.Value;
        if (expireAt.HasValue) ExpireAt = expireAt.Value;

        UpdatedAt = DateTime.UtcNow;
    }


    public void EnsureIsPublishable()
    {
        if (Status != JobStatus.Draft)
            throw new InvalidOperationException("Only draft jobs can be published.");

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            missing.Add(nameof(Title));

        if (string.IsNullOrWhiteSpace(CompanyName))
            missing.Add(nameof(CompanyName));

        if (string.IsNullOrWhiteSpace(Description))
            missing.Add(nameof(Description));

        if (string.IsNullOrWhiteSpace(Location))
            missing.Add(nameof(Location));

        if (!Enum.IsDefined(EmploymentType))
            missing.Add(nameof(EmploymentType));

        if (!Enum.IsDefined(ExperienceLevel))
            missing.Add(nameof(ExperienceLevel));

        if (!Enum.IsDefined(WorkArrangement))
            missing.Add(nameof(WorkArrangement));

        if (RequiredSkills.Count == 0)
            missing.Add(nameof(RequiredSkills));

        if (Vacancies <= 0)
            missing.Add(nameof(Vacancies));

        if (ExpireAt <= DateTime.UtcNow)
            missing.Add(nameof(ExpireAt));

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Job cannot be published. Invalid or missing fields: {string.Join(", ", missing)}"
            );
        }
    }
    public void Publish()
    {
        EnsureIsPublishable();

        Status = JobStatus.Published;
    }
}

using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public class Job : BaseEntity
{
    private readonly List<string> _skillRequirements = new();

    // Required by EF Core.
    private Job()
    {
    }

    public Job(
        Guid employerId,
        string companyName,
        string title)
    {
        if (employerId == Guid.Empty)
            throw new ArgumentException(
                "Employer ID is required.",
                nameof(employerId));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException(
                "Company name is required.",
                nameof(companyName));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Job title is required.",
                nameof(title));

        Id = Guid.NewGuid();
        EmployerId = employerId;
        CompanyName = companyName.Trim();
        Title = title.Trim();

        Status = JobStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid EmployerId { get; private set; }

    public string CompanyName { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public EmploymentType? RoleType { get; private set; }

    public ExperienceLevel? ExperienceLevel { get; private set; }

    public WorkMode? LocationPref { get; private set; }

    public string? City { get; private set; }

    public string? Country { get; private set; }

    public short? MinOverlapHours { get; private set; }

    public string? Description { get; private set; }

    public string? Responsibilities { get; private set; }

    public IReadOnlyCollection<string> SkillRequirements =>
        _skillRequirements.AsReadOnly();

    public JobStatus Status { get; private set; } = JobStatus.Draft;

    public DateTime? ExpiresAt { get; private set; }

    public void UpdateDetails(
        string title,
        string companyName,
        EmploymentType? roleType,
        ExperienceLevel? experienceLevel,
        WorkMode? locationPref,
        string? city,
        string? country,
        short? minOverlapHours,
        string? description,
        string? responsibilities,
        IEnumerable<string>? skills,
        DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Job title is required.",
                nameof(title));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException(
                "Company name is required.",
                nameof(companyName));

        Title = title.Trim();
        CompanyName = companyName.Trim();

        RoleType = roleType;
        ExperienceLevel = experienceLevel;
        LocationPref = locationPref;

        City = city?.Trim();
        Country = country?.Trim();

        MinOverlapHours = minOverlapHours;

        Description = description;
        Responsibilities = responsibilities;

        ExpiresAt = expiresAt;

        _skillRequirements.Clear();

        if (skills is not null)
        {
            foreach (var skill in skills)
            {
                AddSkillInternal(skill);
            }
        }

        Touch();
    }

    public void AddSkill(string skill)
    {
        AddSkillInternal(skill);
        Touch();
    }

    public void RemoveSkill(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return;

        var existingSkill = _skillRequirements
            .FirstOrDefault(s =>
                string.Equals(
                    s,
                    skill,
                    StringComparison.OrdinalIgnoreCase));

        if (existingSkill is null)
            return;

        _skillRequirements.Remove(existingSkill);
        Touch();
    }

    public void Publish()
    {
        if (Status != JobStatus.Draft)
            throw new InvalidOperationException(
                "Only draft jobs can be published.");

        Status = JobStatus.Published;
        Touch();
    }

    public void Expire()
    {
        if (Status != JobStatus.Published)
            throw new InvalidOperationException(
                "Only published jobs can be expired.");

        Status = JobStatus.Expired;
        ExpiresAt = DateTime.UtcNow;

        Touch();
    }

    private void AddSkillInternal(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            throw new ArgumentException(
                "Skill cannot be empty.",
                nameof(skill));

        skill = skill.Trim();

        if (_skillRequirements.Contains(
                skill,
                StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _skillRequirements.Add(skill);
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
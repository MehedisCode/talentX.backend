using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobAlert : BaseEntity
{
    public string TalentId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public EmploymentType? EmploymentType { get; private set; }
    public ExperienceLevel? ExperienceLevel { get; private set; }
    public WorkArrangement? WorkArrangement { get; private set; }
    public bool IsEnabled { get; private set; }

    private JobAlert()
    {
    }

    public static JobAlert Create(
        string talentId,
        string name,
        string title,
        string? location,
        EmploymentType? employmentType,
        ExperienceLevel? experienceLevel,
        WorkArrangement? workArrangement)
    {
        return new JobAlert
        {
            TalentId = talentId,
            Name = name,
            Title = title,
            Location = location,
            EmploymentType = employmentType,
            ExperienceLevel = experienceLevel,
            WorkArrangement = workArrangement,
            IsEnabled = true
        };
    }

    public void Update(
        string name,
        string title,
        string? location,
        EmploymentType? employmentType,
        ExperienceLevel? experienceLevel,
        WorkArrangement? workArrangement)
    {
        Name = name;
        Title = title;
        Location = location;
        EmploymentType = employmentType;
        ExperienceLevel = experienceLevel;
        WorkArrangement = workArrangement;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Toggle(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}

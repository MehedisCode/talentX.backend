using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class Project : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string ProjectName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string ProjectType { get; private set; } = string.Empty;
    public string StartDate { get; private set; } = string.Empty;
    public string? EndDate { get; private set; }
    public bool IsOngoing { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public List<string> TechnologiesUsed { get; private set; } = new();
    public string? ProjectUrl { get; private set; }

    private Project() : base() { }

    public Project(
        string userId,
        string projectName,
        string role,
        string projectType,
        string startDate,
        string? endDate,
        bool isOngoing,
        string description,
        List<string>? technologiesUsed,
        string? projectUrl)
    {
        UserId = userId;
        ProjectName = projectName;
        Role = role;
        ProjectType = projectType;
        StartDate = startDate;
        EndDate = endDate;
        IsOngoing = isOngoing;
        Description = description;
        TechnologiesUsed = technologiesUsed ?? [];
        ProjectUrl = projectUrl;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string projectName,
        string role,
        string projectType,
        string startDate,
        string? endDate,
        bool isOngoing,
        string description,
        List<string>? technologiesUsed,
        string? projectUrl)
    {
        ProjectName = projectName;
        Role = role;
        ProjectType = projectType;
        StartDate = startDate;
        EndDate = endDate;
        IsOngoing = isOngoing;
        Description = description;
        TechnologiesUsed = technologiesUsed ?? [];
        ProjectUrl = projectUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}

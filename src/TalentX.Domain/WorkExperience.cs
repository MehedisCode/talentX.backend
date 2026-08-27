using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class WorkExperience : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;
    public string? StartDate { get; private set; }
    public string? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }

    private WorkExperience() { }

    public WorkExperience(
        string userId,
        string companyName,
        string jobTitle,
        string? startDate = null,
        string? endDate = null,
        bool isCurrent = false)
    {
        UserId = userId;
        CompanyName = companyName;
        JobTitle = jobTitle;
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string companyName,
        string jobTitle,
        string? startDate,
        string? endDate,
        bool isCurrent)
    {
        CompanyName = companyName;
        JobTitle = jobTitle;
        StartDate = startDate;
        EndDate = endDate;
        IsCurrent = isCurrent;
        UpdatedAt = DateTime.UtcNow;
    }
}

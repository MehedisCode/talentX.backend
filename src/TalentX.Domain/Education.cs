using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class Education : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string Institution { get; private set; } = string.Empty;
    public string Degree { get; private set; } = string.Empty;
    public string? StartDate { get; private set; }
    public string? EndDate { get; private set; }

    private Education() { }

    public Education(
        string userId,
        string institution,
        string degree,
        string? startDate = null,
        string? endDate = null)
    {
        UserId = userId;
        Institution = institution;
        Degree = degree;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string institution,
        string degree,
        string? startDate,
        string? endDate)
    {
        Institution = institution;
        Degree = degree;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }
}

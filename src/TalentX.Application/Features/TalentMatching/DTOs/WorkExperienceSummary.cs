namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class WorkExperienceSummary
{
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

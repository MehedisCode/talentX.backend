namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class EducationSummary
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

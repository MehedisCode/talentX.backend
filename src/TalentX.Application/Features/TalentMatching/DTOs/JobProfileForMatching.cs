namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class JobProfileForMatching
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Description { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public string ExperienceLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WorkArrangement { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
}

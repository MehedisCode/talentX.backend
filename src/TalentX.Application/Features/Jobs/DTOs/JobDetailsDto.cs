namespace TalentX.Application.Features.Jobs.DTOs;

public class JobDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Responsibilities { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? RoleType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? LocationPref { get; set; }
    public short? MinOverlapHours { get; set; }
    public IReadOnlyList<string> SkillRequirements { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

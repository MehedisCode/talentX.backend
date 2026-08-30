namespace TalentX.Application.Features.Jobs.DTOs;

public class JobDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string WorkArrangement { get; set; } = string.Empty;
    public IReadOnlyList<string> RequiredSkills { get; set; } = new List<string>();
    public int Vacancies { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? RequiredTimezoneOverlap { get; set; }
}

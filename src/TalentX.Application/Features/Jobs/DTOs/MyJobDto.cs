namespace TalentX.Application.Features.Jobs.DTOs;

public class MyJobDto
{
    public Guid Id { get; set; }
    public string EmployerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WorkArrangement { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int Vacancies { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public short? MinOverlapHours { get; set; }
}

namespace TalentX.Application.Features.Jobs.DTOs;

public class JobDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? WorkMode { get; set; }
    public IReadOnlyList<string> RequiredSkills { get; set; } = new List<string>();
    public int Vacancies { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpireAt { get; set; }
}

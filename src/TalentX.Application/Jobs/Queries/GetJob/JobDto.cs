using TalentX.Domain.Enums;

namespace TalentX.Application.Jobs.Queries.GetJob;

public class JobDto
{
    public Guid Id { get; set; }
    public string EmployerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public ExperienceLevel ExperienceLevel { get; set; }
    public string Location { get; set; } = string.Empty;
    public WorkArrangement WorkArrangement { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public JobStatus Status { get; set; }
    public int Vacancies { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

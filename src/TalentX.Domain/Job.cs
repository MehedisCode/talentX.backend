using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

/// <summary>
/// Job posting. Deliberately anemic — validation and state-transition rules live in
/// TalentX.Application.Features.Jobs.Common.JobRules, not here.
/// </summary>
public class Job : BaseEntity
{
    public Job()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public string EmployerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public string Location { get; set; } = string.Empty;
    public WorkMode WorkMode { get; set; }
    public ExperienceLevel? ExperienceLevel { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public int Vacancies { get; set; }
    public DateTime ExpireAt { get; set; }

    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}

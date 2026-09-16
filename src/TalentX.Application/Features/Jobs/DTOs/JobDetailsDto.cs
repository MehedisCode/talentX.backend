using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public class JobDetailsDto
{
    public Guid Id { get; set; }
    public string EmployerId {get; set;} = string.Empty;
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
    public short? MinOverlapHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public SalaryType? SalaryType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Currency? SalaryCurrency { get; set; }
    public SalaryPeriod? SalaryPeriod { get; set; }
    public string? Requirements { get; set; }
}

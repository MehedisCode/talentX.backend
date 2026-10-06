using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public sealed record AdminJobDetailsDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Requirements { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Country { get; init; }
    public EmploymentType EmploymentType { get; init; }
    public ExperienceLevel ExperienceLevel { get; init; }
    public WorkArrangement WorkArrangement { get; init; }
    public short? MinOverlapHours { get; init; }
    public IReadOnlyList<string> RequiredSkills { get; init; } = new List<string>();
    public int Vacancies { get; init; }

    public SalaryType? SalaryType { get; init; }
    public decimal? SalaryMin { get; init; }
    public decimal? SalaryMax { get; init; }
    public Currency? SalaryCurrency { get; init; }
    public SalaryPeriod? SalaryPeriod { get; init; }

    public JobStatus Status { get; init; }
    public bool IsExpired { get; init; }
    public DateTime CreatedAt { get; init; }
    // When the job last went live (moves on reopen/reactivation).
    public DateTime? PublishedAt { get; init; }
    // When the job was first posted (never changes).
    public DateTime? FirstPublishedAt { get; init; }
    public DateTime ExpireAt { get; init; }

    public string EmployerName { get; init; } = string.Empty;
    public string EmployerEmail { get; init; } = string.Empty;

    // Why the job is down right now. All null once it has been reactivated; the full
    // record of what happened stays in History.
    public DeactivationReason? DeactivationReason { get; init; }
    public string? DeactivationReasonLabel { get; init; }
    public string? DeactivationDetails { get; init; }
    public DateTime? DeactivatedAt { get; init; }

    public IReadOnlyList<JobModerationHistoryDto> History { get; init; } = new List<JobModerationHistoryDto>();
}

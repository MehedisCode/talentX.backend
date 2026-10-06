using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public sealed record AdminJobListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmployerName { get; init; } = string.Empty;
    public string EmployerEmail { get; init; } = string.Empty;
    public JobStatus Status { get; init; }

    // An expired job is still stored as Published, so without this the admin cannot tell
    // a live posting from a dead one.
    public bool IsExpired { get; init; }

    public string Location { get; init; } = string.Empty;
    public EmploymentType EmploymentType { get; init; }
    public WorkArrangement WorkArrangement { get; init; }
    public int ApplicantCount { get; init; }
    // When the job last went live (moves on reopen/reactivation).
    public DateTime? PublishedAt { get; init; }
    // When the job was first posted (never changes).
    public DateTime? FirstPublishedAt { get; init; }
    public DateTime ExpireAt { get; init; }
    public DateTime? DeactivatedAt { get; init; }
}

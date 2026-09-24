namespace TalentX.Application.Features.AdminDashboard.DTOs;

public enum AdminDashboardTrendGranularity
{
    Hour,
    Day,
    Week,
    Month
}

public enum AdminDashboardActivityType
{
    Talent,
    Employer,
    Job
}

public sealed record AdminDashboardDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public AdminDashboardMetricsDto Metrics { get; init; } = new();
    // Invited is counted under Interview; PendingVerification (unverified guest applications) is left out.
    public IReadOnlyList<AdminDashboardApplicationStatusCountDto> ApplicationStatuses { get; init; } = [];
    // Draft jobs are left out.
    public IReadOnlyList<AdminDashboardJobStatusCountDto> JobStatuses { get; init; } = [];
    public IReadOnlyList<AdminDashboardVerificationCountDto> VerificationStatuses { get; init; } = [];
    // Age of the oldest pending employer verification; not limited to the selected range. Null when none are pending.
    public int? OldestPendingDays { get; init; }
    public AdminDashboardTrendDto Trend { get; init; } = new();
    public IReadOnlyList<AdminDashboardActivityDto> Activities { get; init; } = [];
}

public sealed record AdminDashboardMetricsDto
{
    public int TotalTalents { get; init; }
    public int TotalEmployers { get; init; }
    public int VerifiedEmployers { get; init; }
    public int PendingEmployerVerifications { get; init; }
    public int TotalJobs { get; init; }
    public int PublishedJobs { get; init; }
    public int ClosedOrExpiredJobs { get; init; }
    public int TotalApplications { get; init; }
    public int TotalInterviews { get; init; }
    public int TotalHires { get; init; }
}

public sealed record AdminDashboardApplicationStatusCountDto
{
    public ApplicationStatus Status { get; init; }
    public int Count { get; init; }
}

public sealed record AdminDashboardJobStatusCountDto
{
    public JobStatus Status { get; init; }
    public int Count { get; init; }
}

public sealed record AdminDashboardVerificationCountDto
{
    public EmployerVerificationStatus Status { get; init; }
    public int Count { get; init; }
}

public sealed record AdminDashboardTrendDto
{
    public AdminDashboardTrendGranularity Granularity { get; init; }
    public IReadOnlyList<AdminDashboardTrendPointDto> Points { get; init; } = [];
}

public sealed record AdminDashboardTrendPointDto
{
    // Start of the hour/day/week/month bucket, in UTC.
    public DateTime BucketStart { get; init; }
    public int PublishedJobs { get; init; }
    public int Applications { get; init; }
    public int Interviews { get; init; }
    public int Hires { get; init; }
}

public sealed record AdminDashboardActivityDto
{
    public AdminDashboardActivityType Type { get; init; }
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    // When the entity was registered (talent/employer) or published (job), in UTC; the feed is sorted by this.
    public DateTime OccurredAt { get; init; }
    // Only set for Employer activities, so the UI can show a verification badge; null for Talent and Job.
    public EmployerVerificationStatus? VerificationStatus { get; init; }
}

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
    // Counts inside the selected range.
    public AdminDashboardMetricsDto Metrics { get; init; } = new();
    // The same counts over all time, so the admin can see the platform's running totals whatever the range.
    public AdminDashboardMetricsDto Totals { get; init; } = new();
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
    public int DeactivatedJobs { get; init; }
    // Jobs an employer resubmitted after a deactivation and that now wait for an admin decision.
    public int UnderReviewJobs { get; init; }
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
    // The window just before the selected range, the same length and shifted back by whole hours/days/months.
    public DateTime PreviousStartDate { get; init; }
    public DateTime PreviousEndDate { get; init; }
    // The previous window bucket by bucket; it has the same buckets as Points, so they line up by position.
    public IReadOnlyList<AdminDashboardTrendPointDto> PreviousPoints { get; init; } = [];
    // Each series' total over the previous window (the sum of PreviousPoints), so the UI can show the change against it.
    public AdminDashboardTrendTotalsDto PreviousTotals { get; init; } = new();
}

public sealed record AdminDashboardTrendTotalsDto
{
    public int PublishedJobs { get; init; }
    public int Applications { get; init; }
    public int Interviews { get; init; }
    public int Hires { get; init; }
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

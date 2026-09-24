using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.AdminDashboard.Constants;
using TalentX.Application.Features.AdminDashboard.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    // Fixed orders so the UI always gets every status, with zeros where nothing matched.
    private static readonly JobStatus[] JobStatusOrder =
    [
        JobStatus.Published, JobStatus.Closed, JobStatus.Expired, JobStatus.Deactivated, JobStatus.PendingReview
    ];

    private static readonly ApplicationStatus[] ApplicationStatusOrder =
    [
        ApplicationStatus.Applied, ApplicationStatus.Reviewed, ApplicationStatus.Shortlisted,
        ApplicationStatus.Interview, ApplicationStatus.Hired, ApplicationStatus.Rejected
    ];

    private static readonly EmployerVerificationStatus[] VerificationStatusOrder =
    [
        EmployerVerificationStatus.Verified, EmployerVerificationStatus.PendingVerification,
        EmployerVerificationStatus.Rejected
    ];

    private readonly IUnitOfWork _unitOfWork;

    public GetAdminDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (start, end) = ResolveRange(request, now);
        var granularity = ChooseGranularity(end - start);
        var groupByHour = granularity == AdminDashboardTrendGranularity.Hour;

        // Awaited one after another: a single DbContext cannot run queries in parallel.
        var talentCount = await _unitOfWork.Talents.CountCreatedBetweenAsync(start, end, cancellationToken);
        var verificationCounts = await _unitOfWork.Employers
            .GetVerificationStatusCountsCreatedBetweenAsync(start, end, cancellationToken);
        var oldestPending = await _unitOfWork.Employers.GetOldestPendingCreatedAtAsync(cancellationToken);
        var jobCounts = await _unitOfWork.Jobs.GetStatusCountsCreatedBetweenAsync(start, end, cancellationToken);
        var applicationCounts = await _unitOfWork.JobApplications
            .GetStatusCountsAppliedBetweenAsync(start, end, cancellationToken);

        var publishedByBucket = await _unitOfWork.Jobs
            .GetPublishedCountsByBucketAsync(start, end, groupByHour, cancellationToken);
        var appliedByBucket = await _unitOfWork.JobApplications
            .GetAppliedCountsByBucketAsync(start, end, groupByHour, cancellationToken);
        var interviewsByBucket = await _unitOfWork.InterviewInvitations
            .GetCreatedCountsByBucketAsync(start, end, groupByHour, cancellationToken);
        var hiresByBucket = await _unitOfWork.JobApplications
            .GetHiredCountsByBucketAsync(start, end, groupByHour, cancellationToken);

        var recentTalents = await _unitOfWork.Talents
            .GetRecentCreatedBetweenAsync(start, end, AdminDashboardConstants.RecentActivityPerType, cancellationToken);
        var recentEmployers = await _unitOfWork.Employers
            .GetRecentCreatedBetweenAsync(start, end, AdminDashboardConstants.RecentActivityPerType, cancellationToken);
        var recentJobs = await _unitOfWork.Jobs
            .GetRecentPublishedBetweenAsync(start, end, AdminDashboardConstants.RecentActivityPerType, cancellationToken);

        var jobStatuses = JobStatusOrder
            .Select(status => new AdminDashboardJobStatusCountDto
            {
                Status = status,
                Count = jobCounts.GetValueOrDefault(status)
            })
            .ToList();

        // Invited (interview invitation sent) is shown under Interview; PendingVerification never reaches here.
        var applicationStatuses = ApplicationStatusOrder
            .Select(status => new AdminDashboardApplicationStatusCountDto
            {
                Status = status,
                Count = status == ApplicationStatus.Interview
                    ? applicationCounts.GetValueOrDefault(ApplicationStatus.Interview)
                      + applicationCounts.GetValueOrDefault(ApplicationStatus.Invited)
                    : applicationCounts.GetValueOrDefault(status)
            })
            .ToList();

        var verificationStatuses = VerificationStatusOrder
            .Select(status => new AdminDashboardVerificationCountDto
            {
                Status = status,
                Count = verificationCounts.GetValueOrDefault(status)
            })
            .ToList();

        var metrics = new AdminDashboardMetricsDto
        {
            TotalTalents = talentCount,
            TotalEmployers = verificationStatuses.Sum(s => s.Count),
            VerifiedEmployers = verificationCounts.GetValueOrDefault(EmployerVerificationStatus.Verified),
            PendingEmployerVerifications = verificationCounts.GetValueOrDefault(EmployerVerificationStatus.PendingVerification),
            TotalJobs = jobStatuses.Sum(s => s.Count),
            PublishedJobs = jobCounts.GetValueOrDefault(JobStatus.Published),
            ClosedOrExpiredJobs = jobCounts.GetValueOrDefault(JobStatus.Closed) + jobCounts.GetValueOrDefault(JobStatus.Expired),
            TotalApplications = applicationStatuses.Sum(s => s.Count),
            TotalInterviews = interviewsByBucket.Values.Sum(),
            TotalHires = applicationStatuses.Single(s => s.Status == ApplicationStatus.Hired).Count
        };

        return new AdminDashboardDto
        {
            StartDate = start,
            EndDate = end,
            Metrics = metrics,
            ApplicationStatuses = applicationStatuses,
            JobStatuses = jobStatuses,
            VerificationStatuses = verificationStatuses,
            OldestPendingDays = oldestPending.HasValue
                ? Math.Max(0, (int)(now - oldestPending.Value).TotalDays)
                : null,
            Trend = BuildTrend(
                start,
                end,
                granularity,
                publishedByBucket,
                appliedByBucket,
                interviewsByBucket,
                hiresByBucket),
            Activities = BuildActivities(recentTalents, recentEmployers, recentJobs)
        };
    }

    // Instants are used as sent. A missing end means now; a missing start means 30 days before the end.
    // A start further back than the maximum range is pulled forward so a lone StartDate cannot scan everything.
    private static (DateTime Start, DateTime End) ResolveRange(GetAdminDashboardQuery request, DateTime now)
    {
        var end = request.EndDate.HasValue ? ToUtc(request.EndDate.Value) : now;
        var start = request.StartDate.HasValue
            ? ToUtc(request.StartDate.Value)
            : end.AddDays(-AdminDashboardConstants.DefaultRangeDays);

        var earliest = end.AddDays(-AdminDashboardConstants.MaxRangeDays);
        if (start < earliest)
        {
            start = earliest;
        }

        return (start, end);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static AdminDashboardTrendGranularity ChooseGranularity(TimeSpan span)
    {
        if (span <= TimeSpan.FromDays(2)) return AdminDashboardTrendGranularity.Hour;
        if (span <= TimeSpan.FromDays(30)) return AdminDashboardTrendGranularity.Day;
        if (span <= TimeSpan.FromDays(90)) return AdminDashboardTrendGranularity.Week;
        return AdminDashboardTrendGranularity.Month;
    }

    private static AdminDashboardTrendDto BuildTrend(
        DateTime start,
        DateTime end,
        AdminDashboardTrendGranularity granularity,
        IReadOnlyDictionary<DateTime, int> published,
        IReadOnlyDictionary<DateTime, int> applications,
        IReadOnlyDictionary<DateTime, int> interviews,
        IReadOnlyDictionary<DateTime, int> hires)
    {
        var publishedBuckets = RollUp(published, granularity);
        var applicationBuckets = RollUp(applications, granularity);
        var interviewBuckets = RollUp(interviews, granularity);
        var hireBuckets = RollUp(hires, granularity);

        var points = new List<AdminDashboardTrendPointDto>();
        for (var bucket = BucketStart(start, granularity); bucket <= end; bucket = NextBucket(bucket, granularity))
        {
            points.Add(new AdminDashboardTrendPointDto
            {
                BucketStart = bucket,
                PublishedJobs = publishedBuckets.GetValueOrDefault(bucket),
                Applications = applicationBuckets.GetValueOrDefault(bucket),
                Interviews = interviewBuckets.GetValueOrDefault(bucket),
                Hires = hireBuckets.GetValueOrDefault(bucket)
            });
        }

        return new AdminDashboardTrendDto { Granularity = granularity, Points = points };
    }

    // Repositories return hour or day buckets; weeks and months are summed up from the day buckets here.
    private static Dictionary<DateTime, int> RollUp(
        IReadOnlyDictionary<DateTime, int> counts,
        AdminDashboardTrendGranularity granularity)
    {
        var result = new Dictionary<DateTime, int>();
        foreach (var (timestamp, count) in counts)
        {
            var bucket = BucketStart(timestamp, granularity);
            result[bucket] = result.GetValueOrDefault(bucket) + count;
        }

        return result;
    }

    // Weeks start on Monday and months on the 1st, all in UTC.
    private static DateTime BucketStart(DateTime timestamp, AdminDashboardTrendGranularity granularity)
    {
        var utc = ToUtc(timestamp);
        return granularity switch
        {
            AdminDashboardTrendGranularity.Hour => new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc),
            AdminDashboardTrendGranularity.Day => utc.Date,
            AdminDashboardTrendGranularity.Week => utc.Date.AddDays(-(((int)utc.DayOfWeek + 6) % 7)),
            _ => new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static DateTime NextBucket(DateTime bucket, AdminDashboardTrendGranularity granularity) => granularity switch
    {
        AdminDashboardTrendGranularity.Hour => bucket.AddHours(1),
        AdminDashboardTrendGranularity.Day => bucket.AddDays(1),
        AdminDashboardTrendGranularity.Week => bucket.AddDays(7),
        _ => bucket.AddMonths(1)
    };

    private static List<AdminDashboardActivityDto> BuildActivities(
        IEnumerable<Talent> talents,
        IEnumerable<Employer> employers,
        IEnumerable<Job> jobs)
    {
        var activities = new List<AdminDashboardActivityDto>();

        activities.AddRange(talents.Select(talent => new AdminDashboardActivityDto
        {
            Type = AdminDashboardActivityType.Talent,
            Id = talent.Id,
            Title = talent.Name,
            Detail = Join(talent.Tagline, talent.Location),
            OccurredAt = talent.CreatedAt
        }));

        activities.AddRange(employers.Select(employer => new AdminDashboardActivityDto
        {
            Type = AdminDashboardActivityType.Employer,
            Id = employer.Id,
            Title = employer.CompanyName,
            Detail = employer.Name,
            OccurredAt = employer.CreatedAt,
            VerificationStatus = employer.VerificationStatus
        }));

        activities.AddRange(jobs.Select(job => new AdminDashboardActivityDto
        {
            Type = AdminDashboardActivityType.Job,
            Id = job.Id,
            Title = job.Title,
            Detail = Join(job.CompanyName, job.Location),
            OccurredAt = job.PublishedAt ?? job.CreatedAt
        }));

        return activities
            .OrderByDescending(activity => activity.OccurredAt)
            .Take(AdminDashboardConstants.RecentActivityLimit)
            .ToList();
    }

    private static string Join(params string?[] parts) =>
        string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}

using System.Reflection;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.AdminDashboard.Constants;
using TalentX.Application.Features.AdminDashboard.DTOs;
using TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.AdminDashboard.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandlerTests
{
    private static readonly IReadOnlyDictionary<DateTime, int> NoBuckets = new Dictionary<DateTime, int>();
    private static readonly DateTime AllTimeStart = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITalentRepository> _talents = new();
    private readonly Mock<IEmployerRepository> _employers = new();
    private readonly Mock<IJobRepository> _jobs = new();
    private readonly Mock<IJobApplicationRepository> _applications = new();
    private readonly Mock<IInterviewInvitationRepository> _interviews = new();
    private readonly GetAdminDashboardQueryHandler _handler;

    public GetAdminDashboardQueryHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Talents).Returns(_talents.Object);
        _unitOfWork.SetupGet(u => u.Employers).Returns(_employers.Object);
        _unitOfWork.SetupGet(u => u.Jobs).Returns(_jobs.Object);
        _unitOfWork.SetupGet(u => u.JobApplications).Returns(_applications.Object);
        _unitOfWork.SetupGet(u => u.InterviewInvitations).Returns(_interviews.Object);

        _talents.Setup(r => r.CountCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _talents.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent>());

        _employers.Setup(r => r.GetVerificationStatusCountsCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<EmployerVerificationStatus, int>());
        _employers.Setup(r => r.GetOldestPendingCreatedAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);
        _employers.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employer>());

        _jobs.Setup(r => r.GetStatusCountsCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>());
        _jobs.Setup(r => r.GetPublishedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NoBuckets);
        _jobs.Setup(r => r.GetRecentPublishedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        _applications.Setup(r => r.GetStatusCountsAppliedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ApplicationStatus, int>());
        _applications.Setup(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NoBuckets);
        _applications.Setup(r => r.GetHiredCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NoBuckets);

        _interviews.Setup(r => r.GetCreatedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NoBuckets);
        _interviews.Setup(r => r.CountCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _handler = new GetAdminDashboardQueryHandler(_unitOfWork.Object);
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static void SetProperty(object entity, string name, object value)
    {
        var property = entity.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)!;
        property.SetValue(entity, value);
    }

    private static Job NewJob(string title, string company, string location, DateTime publishedAt)
    {
        var job = Job.Create(
            "employer-1", title, company, "Build and maintain backend services.",
            EmploymentType.FullTime, ExperienceLevel.MidLevel, location,
            WorkArrangement.RemoteButAccessible, ["C#"], 2, publishedAt.AddDays(30));
        SetProperty(job, nameof(Job.PublishedAt), publishedAt);
        return job;
    }

    private static Talent NewTalent(string name, DateTime createdAt, string? tagline = null, string? location = null)
    {
        var talent = new Talent(Guid.NewGuid().ToString(), name, $"{name}@example.com".Replace(' ', '.'));
        if (tagline is not null || location is not null)
        {
            talent.UpdateProfile(name, tagline, null, location, null, null, null, null, null, null);
        }

        SetProperty(talent, nameof(Talent.CreatedAt), createdAt);
        return talent;
    }

    private static Employer NewEmployer(string company, string contact, DateTime createdAt)
    {
        var employer = new Employer(Guid.NewGuid().ToString(), contact, $"{company}@example.com", company);
        SetProperty(employer, nameof(Employer.CreatedAt), createdAt);
        return employer;
    }

    [Fact]
    public async Task Handle_WhenNothingExists_ReturnsZeroMetricsAndEveryStatusWithZero()
    {
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 1), EndDate = Utc(2026, 9, 10) },
            CancellationToken.None);

        result.Metrics.Should().BeEquivalentTo(new AdminDashboardMetricsDto());
        result.Totals.Should().BeEquivalentTo(new AdminDashboardMetricsDto());
        result.ApplicationStatuses.Select(s => s.Status).Should().Equal(
            ApplicationStatus.Applied, ApplicationStatus.Reviewed, ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview, ApplicationStatus.Hired, ApplicationStatus.Rejected);
        result.JobStatuses.Select(s => s.Status).Should().Equal(
            JobStatus.Published, JobStatus.Closed, JobStatus.Expired, JobStatus.Deactivated, JobStatus.PendingReview);
        result.VerificationStatuses.Select(s => s.Status).Should().Equal(
            EmployerVerificationStatus.Verified, EmployerVerificationStatus.PendingVerification,
            EmployerVerificationStatus.Rejected);
        result.ApplicationStatuses.Should().OnlyContain(s => s.Count == 0);
        result.Activities.Should().BeEmpty();
        result.OldestPendingDays.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FoldsInvitedIntoInterviewAndComputesApplicationTotals()
    {
        _applications.Setup(r => r.GetStatusCountsAppliedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ApplicationStatus, int>
            {
                [ApplicationStatus.Applied] = 5,
                [ApplicationStatus.Reviewed] = 3,
                [ApplicationStatus.Shortlisted] = 2,
                [ApplicationStatus.Interview] = 1,
                [ApplicationStatus.Invited] = 2,
                [ApplicationStatus.Hired] = 4,
                [ApplicationStatus.Rejected] = 6
            });

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.ApplicationStatuses.Single(s => s.Status == ApplicationStatus.Interview).Count.Should().Be(3);
        result.ApplicationStatuses.Should().NotContain(s => s.Status == ApplicationStatus.Invited);
        result.Metrics.TotalApplications.Should().Be(23);
        result.Metrics.TotalHires.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ComputesJobAndEmployerMetrics()
    {
        _talents.Setup(r => r.CountCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        _employers.Setup(r => r.GetVerificationStatusCountsCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<EmployerVerificationStatus, int>
            {
                [EmployerVerificationStatus.Verified] = 7,
                [EmployerVerificationStatus.PendingVerification] = 3,
                [EmployerVerificationStatus.Rejected] = 1
            });
        _jobs.Setup(r => r.GetStatusCountsCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>
            {
                [JobStatus.Published] = 10,
                [JobStatus.Closed] = 4,
                [JobStatus.Expired] = 3,
                [JobStatus.Deactivated] = 2,
                [JobStatus.PendingReview] = 1
            });

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.Metrics.TotalTalents.Should().Be(12);
        result.Metrics.TotalEmployers.Should().Be(11);
        result.Metrics.VerifiedEmployers.Should().Be(7);
        result.Metrics.PendingEmployerVerifications.Should().Be(3);
        result.Metrics.TotalJobs.Should().Be(20);
        result.Metrics.PublishedJobs.Should().Be(10);
        result.Metrics.ClosedOrExpiredJobs.Should().Be(7);
        result.Metrics.DeactivatedJobs.Should().Be(2);
        result.Metrics.UnderReviewJobs.Should().Be(1);
    }

    [Fact]
    public async Task Handle_TotalInterviewsIsTheSumOfInterviewBuckets()
    {
        _interviews.Setup(r => r.GetCreatedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int> { [Utc(2026, 9, 2)] = 3, [Utc(2026, 9, 5)] = 4 });

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 1), EndDate = Utc(2026, 9, 10) },
            CancellationToken.None);

        result.Metrics.TotalInterviews.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ReturnsAllTimeTotalsThatDoNotDependOnTheRequestedRange()
    {
        var rangeStart = Utc(2026, 9, 1);
        var rangeEnd = Utc(2026, 9, 10);

        // In range.
        _talents.Setup(r => r.CountCreatedBetweenAsync(rangeStart, rangeEnd, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _employers.Setup(r => r.GetVerificationStatusCountsCreatedBetweenAsync(rangeStart, rangeEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<EmployerVerificationStatus, int> { [EmployerVerificationStatus.Verified] = 2 });
        _jobs.Setup(r => r.GetStatusCountsCreatedBetweenAsync(rangeStart, rangeEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int> { [JobStatus.Published] = 3 });
        _applications.Setup(r => r.GetStatusCountsAppliedBetweenAsync(rangeStart, rangeEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ApplicationStatus, int> { [ApplicationStatus.Applied] = 7 });
        _interviews.Setup(r => r.GetCreatedCountsByBucketAsync(rangeStart, rangeEnd, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int> { [Utc(2026, 9, 2)] = 3 });

        // All time.
        _talents.Setup(r => r.CountCreatedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _employers.Setup(r => r.GetVerificationStatusCountsCreatedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<EmployerVerificationStatus, int>
            {
                [EmployerVerificationStatus.Verified] = 20,
                [EmployerVerificationStatus.PendingVerification] = 4,
                [EmployerVerificationStatus.Rejected] = 1
            });
        _jobs.Setup(r => r.GetStatusCountsCreatedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>
            {
                [JobStatus.Published] = 30,
                [JobStatus.Closed] = 10,
                [JobStatus.Expired] = 5,
                [JobStatus.Deactivated] = 2,
                [JobStatus.PendingReview] = 1
            });
        _applications.Setup(r => r.GetStatusCountsAppliedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<ApplicationStatus, int>
            {
                [ApplicationStatus.Applied] = 50,
                [ApplicationStatus.Interview] = 6,
                [ApplicationStatus.Invited] = 4,
                [ApplicationStatus.Hired] = 9,
                [ApplicationStatus.Rejected] = 11
            });
        _interviews.Setup(r => r.CountCreatedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(40);

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = rangeStart, EndDate = rangeEnd },
            CancellationToken.None);

        // The range block is unchanged.
        result.Metrics.TotalTalents.Should().Be(5);
        result.Metrics.TotalEmployers.Should().Be(2);
        result.Metrics.TotalJobs.Should().Be(3);
        result.Metrics.TotalApplications.Should().Be(7);
        result.Metrics.TotalInterviews.Should().Be(3);

        // The totals cover everything, with the same rules (Invited under Interview, drafts left out).
        result.Totals.TotalTalents.Should().Be(100);
        result.Totals.TotalEmployers.Should().Be(25);
        result.Totals.VerifiedEmployers.Should().Be(20);
        result.Totals.PendingEmployerVerifications.Should().Be(4);
        result.Totals.TotalJobs.Should().Be(48);
        result.Totals.PublishedJobs.Should().Be(30);
        result.Totals.ClosedOrExpiredJobs.Should().Be(15);
        result.Totals.DeactivatedJobs.Should().Be(2);
        result.Totals.UnderReviewJobs.Should().Be(1);
        result.Totals.TotalApplications.Should().Be(80);
        result.Totals.TotalInterviews.Should().Be(40);
        result.Totals.TotalHires.Should().Be(9);
    }

    [Fact]
    public async Task Handle_QueriesTheAllTimeTotalsFromAUtcStartUntilNow()
    {
        var before = DateTime.UtcNow;

        await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 1), EndDate = Utc(2026, 9, 10) },
            CancellationToken.None);

        _talents.Verify(
            r => r.CountCreatedBetweenAsync(
                It.Is<DateTime>(start => start == AllTimeStart && start.Kind == DateTimeKind.Utc),
                It.Is<DateTime>(end => end >= before && end <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _interviews.Verify(
            r => r.CountCreatedBetweenAsync(AllTimeStart, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutDates_UsesTheLastThirtyCalendarDaysIncludingToday()
    {
        var before = DateTime.UtcNow;

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.EndDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        result.StartDate.Should().Be(result.EndDate.Date.AddDays(-29));
        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Day);
        result.Trend.Points.Should().HaveCount(30);
    }

    [Fact]
    public async Task Handle_WithOnlyAStartDateFarInThePast_PullsTheStartForwardToTheMaximumRange()
    {
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = DateTime.UtcNow.AddYears(-10) },
            CancellationToken.None);

        (result.EndDate - result.StartDate).Should().Be(TimeSpan.FromDays(366));
    }

    [Theory]
    [InlineData(1, AdminDashboardTrendGranularity.Hour)]
    [InlineData(2, AdminDashboardTrendGranularity.Hour)]
    [InlineData(7, AdminDashboardTrendGranularity.Day)]
    [InlineData(30, AdminDashboardTrendGranularity.Day)]
    [InlineData(31, AdminDashboardTrendGranularity.Week)]
    [InlineData(90, AdminDashboardTrendGranularity.Week)]
    [InlineData(91, AdminDashboardTrendGranularity.Month)]
    [InlineData(366, AdminDashboardTrendGranularity.Month)]
    public async Task Handle_ChoosesGranularityFromTheRangeLength(int days, AdminDashboardTrendGranularity expected)
    {
        var end = Utc(2026, 9, 20, 12);

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = end.AddDays(-days), EndDate = end },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithHourlyGranularity_AsksRepositoriesForHourBuckets()
    {
        var end = Utc(2026, 9, 11, 10, 30);

        await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = end.AddHours(-24), EndDate = end },
            CancellationToken.None);

        _jobs.Verify(r => r.GetPublishedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _applications.Verify(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _applications.Verify(r => r.GetHiredCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _interviews.Verify(r => r.GetCreatedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDailyGranularity_AsksRepositoriesForDayBuckets()
    {
        await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 1), EndDate = Utc(2026, 9, 10) },
            CancellationToken.None);

        _jobs.Verify(r => r.GetPublishedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>()), Times.Once);
        _applications.Verify(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DailyTrend_ZeroFillsBucketsWithNoData()
    {
        _applications.Setup(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int> { [Utc(2026, 9, 2)] = 4 });

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 1), EndDate = Utc(2026, 9, 3, 12) },
            CancellationToken.None);

        result.Trend.Points.Select(p => p.BucketStart).Should().Equal(Utc(2026, 9, 1), Utc(2026, 9, 2), Utc(2026, 9, 3));
        result.Trend.Points.Select(p => p.Applications).Should().Equal(0, 4, 0);
        result.Trend.Points.Should().OnlyContain(p => p.PublishedJobs == 0 && p.Interviews == 0 && p.Hires == 0);
    }

    [Fact]
    public async Task Handle_HourlyTrend_StartsAtTheHourContainingTheStartAndPlacesCounts()
    {
        var end = Utc(2026, 9, 11, 10, 30);
        _jobs.Setup(r => r.GetPublishedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int> { [Utc(2026, 9, 10, 12)] = 2 });

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = end.AddHours(-24), EndDate = end },
            CancellationToken.None);

        result.Trend.Points.Should().HaveCount(25);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2026, 9, 10, 10));
        result.Trend.Points.Last().BucketStart.Should().Be(Utc(2026, 9, 11, 10));
        result.Trend.Points.Single(p => p.BucketStart == Utc(2026, 9, 10, 12)).PublishedJobs.Should().Be(2);
        result.Trend.Points.Sum(p => p.PublishedJobs).Should().Be(2);
    }

    [Fact]
    public async Task Handle_WeeklyTrend_SumsDaysIntoSevenDayBlocksFromTheStart()
    {
        _applications.Setup(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int>
            {
                [Utc(2026, 7, 8)] = 1,
                [Utc(2026, 7, 14)] = 2,
                [Utc(2026, 7, 15)] = 5
            });

        // 2026-07-08 is a Wednesday: weeks run Wed-Tue from the start, not Mon-Sun.
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 7, 8), EndDate = Utc(2026, 9, 4) },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Week);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2026, 7, 8));
        result.Trend.Points.Single(p => p.BucketStart == Utc(2026, 7, 8)).Applications.Should().Be(3);
        result.Trend.Points.Single(p => p.BucketStart == Utc(2026, 7, 15)).Applications.Should().Be(5);
    }

    [Fact]
    public async Task Handle_AlignedTwentyFourHours_ReturnsTwentyFourHourlyPointsEndingWithTheCurrentHour()
    {
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 9, 23, 13), EndDate = Utc(2026, 9, 24, 12, 30) },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Hour);
        result.Trend.Points.Should().HaveCount(24);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2026, 9, 23, 13));
        result.Trend.Points.Last().BucketStart.Should().Be(Utc(2026, 9, 24, 12));
    }

    [Fact]
    public async Task Handle_AlignedNinetyDays_ReturnsThirteenWeeklyPointsStartingOnTheStartDay()
    {
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 6, 27), EndDate = Utc(2026, 9, 24, 12) },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Week);
        result.Trend.Points.Should().HaveCount(13);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2026, 6, 27));
        result.Trend.Points.Last().BucketStart.Should().Be(Utc(2026, 9, 19));
    }

    [Fact]
    public async Task Handle_AlignedTwelveMonths_ReturnsTwelveMonthlyPointsEndingWithTheCurrentMonth()
    {
        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2025, 10, 1), EndDate = Utc(2026, 9, 24, 12) },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Month);
        result.Trend.Points.Should().HaveCount(12);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2025, 10, 1));
        result.Trend.Points.Last().BucketStart.Should().Be(Utc(2026, 9, 1));
    }

    [Fact]
    public async Task Handle_MonthlyTrend_SumsDaysIntoMonthsStartingOnTheFirst()
    {
        _applications.Setup(r => r.GetAppliedCountsByBucketAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateTime, int>
            {
                [Utc(2026, 1, 20)] = 1,
                [Utc(2026, 1, 25)] = 2,
                [Utc(2026, 2, 3)] = 4
            });

        var result = await _handler.Handle(
            new GetAdminDashboardQuery { StartDate = Utc(2026, 1, 15), EndDate = Utc(2026, 9, 15) },
            CancellationToken.None);

        result.Trend.Granularity.Should().Be(AdminDashboardTrendGranularity.Month);
        result.Trend.Points.Should().HaveCount(9);
        result.Trend.Points.First().BucketStart.Should().Be(Utc(2026, 1, 1));
        result.Trend.Points.Single(p => p.BucketStart == Utc(2026, 1, 1)).Applications.Should().Be(3);
        result.Trend.Points.Single(p => p.BucketStart == Utc(2026, 2, 1)).Applications.Should().Be(4);
    }

    [Fact]
    public async Task Handle_OldestPendingDaysIsWholeDaysSinceTheOldestPendingEmployer()
    {
        _employers.Setup(r => r.GetOldestPendingCreatedAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddDays(-5).AddHours(-3));

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.OldestPendingDays.Should().Be(5);
    }

    [Fact]
    public async Task Handle_MergesActivitiesNewestFirstAndMapsEachType()
    {
        var talent = NewTalent("Aisha Rahman", Utc(2026, 9, 10, 9), tagline: "Data Analyst", location: "Dubai");
        var employer = NewEmployer("Brightline Logistics", "Omar Khan", Utc(2026, 9, 10, 11));
        var job = NewJob("Senior Designer", "Northwind Labs", "Remote", Utc(2026, 9, 10, 10));

        _talents.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { talent });
        _employers.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employer> { employer });
        _jobs.Setup(r => r.GetRecentPublishedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.Activities.Select(a => a.Type).Should().Equal(
            AdminDashboardActivityType.Employer, AdminDashboardActivityType.Job, AdminDashboardActivityType.Talent);

        var employerActivity = result.Activities[0];
        employerActivity.Id.Should().Be(employer.Id);
        employerActivity.Title.Should().Be("Brightline Logistics");
        employerActivity.Detail.Should().Be("Omar Khan");
        employerActivity.VerificationStatus.Should().Be(EmployerVerificationStatus.PendingVerification);

        var jobActivity = result.Activities[1];
        jobActivity.Title.Should().Be("Senior Designer");
        jobActivity.Detail.Should().Be("Northwind Labs · Remote");
        jobActivity.OccurredAt.Should().Be(Utc(2026, 9, 10, 10));
        jobActivity.VerificationStatus.Should().BeNull();

        var talentActivity = result.Activities[2];
        talentActivity.Title.Should().Be("Aisha Rahman");
        talentActivity.Detail.Should().Be("Data Analyst · Dubai");
        talentActivity.VerificationStatus.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTalentHasNoTaglineOrLocation_LeavesTheDetailEmpty()
    {
        _talents.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Talent> { NewTalent("Plain Talent", Utc(2026, 9, 10)) });

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.Activities.Should().ContainSingle().Which.Detail.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TrimsTheMergedActivitiesToTheRecentActivityLimit()
    {
        var perType = AdminDashboardConstants.RecentActivityPerType;
        var limit = AdminDashboardConstants.RecentActivityLimit;

        var talents = Enumerable.Range(0, perType + 2).Select(i => NewTalent($"Talent {i}", Utc(2026, 9, 10, 0, i))).ToList();
        var employers = Enumerable.Range(0, perType + 2).Select(i => NewEmployer($"Company {i}", $"Owner {i}", Utc(2026, 9, 10, 1, i))).ToList();
        var jobs = Enumerable.Range(0, perType + 2).Select(i => NewJob($"Job {i}", "Acme", "Dhaka", Utc(2026, 9, 10, 2, i))).ToList();

        _talents.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime start, DateTime end, int take, CancellationToken token) => talents.Take(take).ToList());
        _employers.Setup(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime start, DateTime end, int take, CancellationToken token) => employers.Take(take).ToList());
        _jobs.Setup(r => r.GetRecentPublishedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime start, DateTime end, int take, CancellationToken token) => jobs.Take(take).ToList());

        var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

        result.Activities.Should().HaveCount(limit);
        result.Activities.Should().BeInDescendingOrder(a => a.OccurredAt);
        result.Activities.Count(a => a.Type == AdminDashboardActivityType.Job).Should().Be(perType);
        // Jobs are newest, then employers, then talents, so the limit takes every job and fills the rest with employers.
        result.Activities.Count(a => a.Type == AdminDashboardActivityType.Employer)
            .Should().Be(limit - perType);
        result.Activities.Should().NotContain(a => a.Type == AdminDashboardActivityType.Talent);

        _talents.Verify(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), perType, It.IsAny<CancellationToken>()), Times.Once);
        _employers.Verify(r => r.GetRecentCreatedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), perType, It.IsAny<CancellationToken>()), Times.Once);
        _jobs.Verify(r => r.GetRecentPublishedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), perType, It.IsAny<CancellationToken>()), Times.Once);
    }
}

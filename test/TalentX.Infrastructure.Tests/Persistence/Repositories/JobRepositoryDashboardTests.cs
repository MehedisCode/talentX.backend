using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobRepositoryDashboardTests
{
    private static readonly DateTime Base = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job AddJob(
        ApplicationDbContext context,
        JobStatus status,
        DateTime createdAt,
        DateTime? publishedAt = null,
        string title = "Backend Engineer",
        DateTime? expireAt = null,
        DateTime? firstPublishedAt = null)
    {
        var job = Job.Create(
            "employer-1",
            title,
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            2,
            // Relative to now, not to the fixed test dates: expiry is judged against the real clock.
            DateTime.UtcNow.AddDays(60));

        switch (status)
        {
            case JobStatus.Published:
                job.MarkAsPublished();
                break;
            case JobStatus.Closed:
                job.MarkAsPublished();
                job.MarkAsClosed();
                break;
            case JobStatus.Deactivated:
                job.MarkAsPublished();
                job.Deactivate(DeactivationReason.Spam, "Posting spam links to another site.");
                break;
        }

        context.Jobs.Add(job);
        context.Entry(job).Property(j => j.CreatedAt).CurrentValue = createdAt;
        if (publishedAt.HasValue)
        {
            context.Entry(job).Property(j => j.PublishedAt).CurrentValue = publishedAt;
        }

        // Published jobs were first posted at the given first-publish time, else their publish time, else creation.
        if (status != JobStatus.Draft)
        {
            context.Entry(job).Property(j => j.FirstPublishedAt).CurrentValue = firstPublishedAt ?? publishedAt ?? createdAt;
        }

        // Set after publishing, because publishing refuses a deadline that has already passed.
        if (expireAt.HasValue)
        {
            context.Entry(job).Property(j => j.ExpireAt).CurrentValue = expireAt.Value;
        }

        return job;
    }

    [Fact]
    public async Task GetStatusCountsFirstPublishedBetweenAsync_ExcludesDraftsDeletedJobsAndOutOfRangeJobs()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddHours(-1));
        AddJob(context, JobStatus.Published, Base.AddHours(-2));
        AddJob(context, JobStatus.Closed, Base.AddHours(-3));
        AddJob(context, JobStatus.Deactivated, Base.AddHours(-4));
        AddJob(context, JobStatus.Draft, Base.AddHours(-5));
        AddJob(context, JobStatus.Published, Base.AddDays(-20));
        var deleted = AddJob(context, JobStatus.Published, Base.AddHours(-6));
        deleted.SoftDelete();
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Published].Should().Be(2);
        counts[JobStatus.Closed].Should().Be(1);
        counts[JobStatus.Deactivated].Should().Be(1);
        counts.Should().NotContainKey(JobStatus.Draft);
    }

    [Fact]
    public async Task GetStatusCountsFirstPublishedBetweenAsync_CountsAPublishedJobPastItsDeadlineAsExpired()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddHours(-1));
        AddJob(context, JobStatus.Published, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-3));
        AddJob(context, JobStatus.Published, Base.AddHours(-3), expireAt: DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Published].Should().Be(1);
        counts[JobStatus.Expired].Should().Be(2);
    }

    [Fact]
    public async Task GetStatusCountsFirstPublishedBetweenAsync_AddsExplicitlyExpiredJobsToThoseDerivedFromTheDeadline()
    {
        await using var context = NewContext();
        var stored = AddJob(context, JobStatus.Published, Base.AddHours(-1));
        stored.MarkAsExpired();
        AddJob(context, JobStatus.Published, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-1));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Expired].Should().Be(2);
        counts.Should().NotContainKey(JobStatus.Published);
    }

    [Fact]
    public async Task GetStatusCountsFirstPublishedBetweenAsync_DoesNotTurnOtherStatusesIntoExpiredJustBecauseTheDeadlinePassed()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Closed, Base.AddHours(-1), expireAt: DateTime.UtcNow.AddDays(-5));
        AddJob(context, JobStatus.Deactivated, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-5));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Closed].Should().Be(1);
        counts[JobStatus.Deactivated].Should().Be(1);
        counts.Should().NotContainKey(JobStatus.Expired);
    }

    [Fact]
    public async Task GetPublishedCountsByBucketAsync_GroupsByDay()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddDays(-1).AddHours(-3));
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddDays(-1).AddHours(2));
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base);
        AddJob(context, JobStatus.Published, Base.AddDays(-50), publishedAt: Base.AddDays(-30));
        AddJob(context, JobStatus.Draft, Base.AddDays(-1));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var buckets = await repository.GetPublishedCountsByBucketAsync(Base.AddDays(-2), Base, groupByHour: false);

        buckets.Should().HaveCount(2);
        buckets[Base.AddDays(-1).Date].Should().Be(2);
        buckets[Base.Date].Should().Be(1);
    }

    [Fact]
    public async Task GetPublishedCountsByBucketAsync_GroupsByHour()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddDays(-1), publishedAt: Base.AddHours(-2).AddMinutes(10));
        AddJob(context, JobStatus.Published, Base.AddDays(-1), publishedAt: Base.AddHours(-2).AddMinutes(50));
        AddJob(context, JobStatus.Published, Base.AddDays(-1), publishedAt: Base.AddHours(-1));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var buckets = await repository.GetPublishedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: true);

        buckets.Should().HaveCount(2);
        buckets[Base.AddHours(-2)].Should().Be(2);
        buckets[Base.AddHours(-1)].Should().Be(1);
    }

    [Fact]
    public async Task GetRecentPublishedBetweenAsync_ReturnsNewestPublishedFirstAndSkipsNeverPublishedJobs()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddHours(-3), title: "Oldest");
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddHours(-1), title: "Newest");
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddHours(-2), title: "Middle");
        AddJob(context, JobStatus.Draft, Base.AddHours(-1), title: "Draft");
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var recent = await repository.GetRecentPublishedBetweenAsync(Base.AddDays(-1), Base, take: 2);

        recent.Select(j => j.Title).Should().Equal("Newest", "Middle");
    }

    [Fact]
    public async Task DashboardQueries_DoNotCountAnOldJobReopenedInsideTheWindowAsANewPosting()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddDays(-40), publishedAt: Base.AddHours(-1),
            firstPublishedAt: Base.AddDays(-40), title: "Reopened");
        AddJob(context, JobStatus.Published, Base.AddDays(-5), publishedAt: Base.AddHours(-2), title: "New");
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);
        var buckets = await repository.GetPublishedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: false);
        var recent = await repository.GetRecentPublishedBetweenAsync(Base.AddDays(-1), Base, take: 5);

        counts.Values.Sum().Should().Be(1);
        buckets.Values.Sum().Should().Be(1);
        recent.Select(j => j.Title).Should().Equal("New");
    }

    [Fact]
    public async Task DashboardQueries_FallBackToCreatedAtForPublishedJobsWithoutAFirstPublishDate()
    {
        await using var context = NewContext();
        var legacy = AddJob(context, JobStatus.Published, Base.AddHours(-3), title: "Legacy");
        context.Entry(legacy).Property(j => j.FirstPublishedAt).CurrentValue = null;
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-1), Base);
        var buckets = await repository.GetPublishedCountsByBucketAsync(Base.AddDays(-1), Base, groupByHour: false);
        var recent = await repository.GetRecentPublishedBetweenAsync(Base.AddDays(-1), Base, take: 5);

        counts.Values.Sum().Should().Be(1);
        buckets[Base.Date].Should().Be(1);
        recent.Select(j => j.Title).Should().Equal("Legacy");
    }

    [Fact]
    public async Task DashboardQueries_TheJobCardTotalMatchesTheTrendTotalForTheSameWindow()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddDays(-3), publishedAt: Base.AddDays(-2));
        AddJob(context, JobStatus.Closed, Base.AddDays(-4), publishedAt: Base.AddDays(-3));
        AddJob(context, JobStatus.Deactivated, Base.AddDays(-5), publishedAt: Base.AddDays(-4));
        AddJob(context, JobStatus.Published, Base.AddDays(-60), publishedAt: Base.AddDays(-1),
            firstPublishedAt: Base.AddDays(-60));
        AddJob(context, JobStatus.Draft, Base.AddDays(-2));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsFirstPublishedBetweenAsync(Base.AddDays(-7), Base);
        var buckets = await repository.GetPublishedCountsByBucketAsync(Base.AddDays(-7), Base, groupByHour: false);

        counts.Values.Sum().Should().Be(3);
        buckets.Values.Sum().Should().Be(counts.Values.Sum());
    }
}

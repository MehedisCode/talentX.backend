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
        DateTime? expireAt = null)
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

        // Set after publishing, because publishing refuses a deadline that has already passed.
        if (expireAt.HasValue)
        {
            context.Entry(job).Property(j => j.ExpireAt).CurrentValue = expireAt.Value;
        }

        return job;
    }

    [Fact]
    public async Task GetStatusCountsCreatedBetweenAsync_ExcludesDraftsDeletedJobsAndOutOfRangeJobs()
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

        var counts = await repository.GetStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Published].Should().Be(2);
        counts[JobStatus.Closed].Should().Be(1);
        counts[JobStatus.Deactivated].Should().Be(1);
        counts.Should().NotContainKey(JobStatus.Draft);
    }

    [Fact]
    public async Task GetStatusCountsCreatedBetweenAsync_CountsAPublishedJobPastItsDeadlineAsExpired()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Published, Base.AddHours(-1));
        AddJob(context, JobStatus.Published, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-3));
        AddJob(context, JobStatus.Published, Base.AddHours(-3), expireAt: DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Published].Should().Be(1);
        counts[JobStatus.Expired].Should().Be(2);
    }

    [Fact]
    public async Task GetStatusCountsCreatedBetweenAsync_AddsExplicitlyExpiredJobsToThoseDerivedFromTheDeadline()
    {
        await using var context = NewContext();
        var stored = AddJob(context, JobStatus.Published, Base.AddHours(-1));
        stored.MarkAsExpired();
        AddJob(context, JobStatus.Published, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-1));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

        counts[JobStatus.Expired].Should().Be(2);
        counts.Should().NotContainKey(JobStatus.Published);
    }

    [Fact]
    public async Task GetStatusCountsCreatedBetweenAsync_DoesNotTurnOtherStatusesIntoExpiredJustBecauseTheDeadlinePassed()
    {
        await using var context = NewContext();
        AddJob(context, JobStatus.Closed, Base.AddHours(-1), expireAt: DateTime.UtcNow.AddDays(-5));
        AddJob(context, JobStatus.Deactivated, Base.AddHours(-2), expireAt: DateTime.UtcNow.AddDays(-5));
        await context.SaveChangesAsync();
        var repository = new JobRepository(context);

        var counts = await repository.GetStatusCountsCreatedBetweenAsync(Base.AddDays(-1), Base);

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
}

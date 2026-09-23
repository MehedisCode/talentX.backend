using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class JobModerationHistoryRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job MakePublishedJob(string title = "Backend Engineer")
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
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    private static JobModerationHistory MakeHistory(
        Guid jobId,
        JobModerationAction action = JobModerationAction.Deactivated,
        JobStatus fromStatus = JobStatus.Published,
        JobStatus toStatus = JobStatus.Deactivated)
    {
        return JobModerationHistory.Create(
            jobId,
            action,
            fromStatus,
            toStatus,
            "admin-1",
            "admin@talentx.com",
            action == JobModerationAction.Deactivated ? DeactivationReason.Spam : null,
            action == JobModerationAction.Deactivated ? "Links to an unrelated external site." : null);
    }

    [Fact]
    public async Task AddAsync_PersistsHistoryWithAllFields()
    {
        await using var context = NewContext();
        var repository = new JobModerationHistoryRepository(context);
        var job = MakePublishedJob();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        await repository.AddAsync(MakeHistory(job.Id));
        await context.SaveChangesAsync();

        var saved = await context.JobModerationHistories.SingleAsync();
        saved.JobId.Should().Be(job.Id);
        saved.Action.Should().Be(JobModerationAction.Deactivated);
        saved.FromStatus.Should().Be(JobStatus.Published);
        saved.ToStatus.Should().Be(JobStatus.Deactivated);
        saved.Reason.Should().Be(DeactivationReason.Spam);
        saved.Details.Should().Be("Links to an unrelated external site.");
        saved.ActorUserId.Should().Be("admin-1");
        saved.ActorEmail.Should().Be("admin@talentx.com");
    }

    [Fact]
    public async Task GetByJobIdAsync_ReturnsOnlyThatJobsHistory_NewestFirst()
    {
        await using var context = NewContext();
        var repository = new JobModerationHistoryRepository(context);
        var job = MakePublishedJob("Job A");
        var otherJob = MakePublishedJob("Job B");
        context.Jobs.AddRange(job, otherJob);
        await context.SaveChangesAsync();

        var deactivated = MakeHistory(job.Id);
        await Task.Delay(5);
        var resubmitted = MakeHistory(job.Id, JobModerationAction.ResubmittedForReview, JobStatus.Deactivated, JobStatus.PendingReview);
        await Task.Delay(5);
        var reactivated = MakeHistory(job.Id, JobModerationAction.Reactivated, JobStatus.PendingReview, JobStatus.Published);
        var otherJobHistory = MakeHistory(otherJob.Id);

        context.JobModerationHistories.AddRange(resubmitted, otherJobHistory, deactivated, reactivated);
        await context.SaveChangesAsync();

        var result = await repository.GetByJobIdAsync(job.Id);

        result.Select(history => history.Action).Should().Equal(
            JobModerationAction.Reactivated,
            JobModerationAction.ResubmittedForReview,
            JobModerationAction.Deactivated);
    }

    [Fact]
    public async Task GetByJobIdAsync_WhenJobHasNoHistory_ReturnsEmpty()
    {
        await using var context = NewContext();
        var repository = new JobModerationHistoryRepository(context);

        var result = await repository.GetByJobIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByJobIdsAsync_DeletesHistoriesMatchingJobIds()
    {
        await using var context = NewContext();
        var repository = new JobModerationHistoryRepository(context);

        var job1 = MakePublishedJob("Job 1");
        var job2 = MakePublishedJob("Job 2");
        var otherJob = MakePublishedJob("Other Job");
        context.Jobs.AddRange(job1, job2, otherJob);

        var h1 = MakeHistory(job1.Id);
        var h2 = MakeHistory(job2.Id);
        var hOther = MakeHistory(otherJob.Id);
        context.JobModerationHistories.AddRange(h1, h2, hOther);
        await context.SaveChangesAsync();

        await repository.DeleteByJobIdsAsync(new[] { job1.Id, job2.Id });
        await context.SaveChangesAsync();

        context.JobModerationHistories.Should().ContainSingle(h => h.JobId == otherJob.Id);
    }
}

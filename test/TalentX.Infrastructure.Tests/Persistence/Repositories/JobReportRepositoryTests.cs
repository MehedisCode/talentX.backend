using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public class JobReportRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static JobReport MakeReport(Guid jobId, string reporterUserId = "talent-1")
    {
        return new JobReport(jobId, reporterUserId, JobReportReason.SuspectedScam, "Details");
    }

    private static Job MakePublishedJob()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
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

    [Fact]
    public async Task AddAsync_PersistsReport()
    {
        await using var context = NewContext();
        var repository = new JobReportRepository(context);
        var job = MakePublishedJob();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var report = MakeReport(job.Id);
        await repository.AddAsync(report);
        await context.SaveChangesAsync();

        var saved = await context.JobReports.SingleAsync();
        saved.JobId.Should().Be(job.Id);
        saved.ReporterUserId.Should().Be("talent-1");
        saved.Reason.Should().Be(JobReportReason.SuspectedScam);
        saved.Status.Should().Be(JobReportStatus.Pending);
    }

    [Fact]
    public async Task DeleteByJobIdsAsync_DeletesOnlyReportsMatchingJobIds()
    {
        await using var context = NewContext();
        var repository = new JobReportRepository(context);
        var job1 = MakePublishedJob();
        var job2 = MakePublishedJob();
        var otherJob = MakePublishedJob();
        context.Jobs.AddRange(job1, job2, otherJob);

        context.JobReports.AddRange(
            MakeReport(job1.Id),
            MakeReport(job2.Id, "talent-2"),
            MakeReport(otherJob.Id));
        await context.SaveChangesAsync();

        await repository.DeleteByJobIdsAsync(new[] { job1.Id, job2.Id });
        await context.SaveChangesAsync();

        context.JobReports.Should().ContainSingle(r => r.JobId == otherJob.Id);
    }

    [Fact]
    public async Task DeleteByJobIdsAsync_WhenIdsEmpty_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new JobReportRepository(context);
        var job = MakePublishedJob();
        context.Jobs.Add(job);
        context.JobReports.Add(MakeReport(job.Id));
        await context.SaveChangesAsync();

        await repository.DeleteByJobIdsAsync(Array.Empty<Guid>());
        await context.SaveChangesAsync();

        context.JobReports.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteByReporterUserIdAsync_DeletesOnlyThatReportersReports()
    {
        await using var context = NewContext();
        var repository = new JobReportRepository(context);
        var job = MakePublishedJob();
        var otherJob = MakePublishedJob();
        context.Jobs.AddRange(job, otherJob);

        context.JobReports.AddRange(
            MakeReport(job.Id, "talent-1"),
            MakeReport(job.Id, "talent-2"),
            MakeReport(otherJob.Id, "talent-2"));
        await context.SaveChangesAsync();

        await repository.DeleteByReporterUserIdAsync("talent-1");
        await context.SaveChangesAsync();

        context.JobReports.Should().OnlyContain(r => r.ReporterUserId == "talent-2");
        context.JobReports.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteByReporterUserIdAsync_WhenReportHasNoReports_DoesNothing()
    {
        await using var context = NewContext();
        var repository = new JobReportRepository(context);
        context.JobReports.Add(MakeReport(Guid.NewGuid(), "talent-1"));
        await context.SaveChangesAsync();

        await repository.DeleteByReporterUserIdAsync("talent-unknown");
        await context.SaveChangesAsync();

        context.JobReports.Should().ContainSingle();
    }
}

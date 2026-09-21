using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

// Deactivated and under-review jobs must never reach talents: not in the public listing,
// not on the public job page (which the apply and save flows also load through).
public class JobRepositoryModerationVisibilityTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Job MakeJob(string title, JobStatus status, string employerId = "employer-1")
    {
        var job = Job.Create(
            employerId,
            title,
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            2,
            DateTime.UtcNow.AddDays(30));

        job.MarkAsPublished();
        if (status is JobStatus.Deactivated or JobStatus.PendingReview)
        {
            job.Deactivate(DeactivationReason.InappropriateContent, "Contains offensive language in the description.");
        }

        if (status == JobStatus.PendingReview)
        {
            job.SubmitForReview();
        }

        return job;
    }

    [Fact]
    public async Task GetPublishedJobsAsync_ExcludesDeactivatedAndPendingReviewJobs()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.Jobs.AddRange(
            MakeJob("Live Job", JobStatus.Published),
            MakeJob("Deactivated Job", JobStatus.Deactivated),
            MakeJob("Pending Job", JobStatus.PendingReview));
        await context.SaveChangesAsync();

        var result = await repository.GetPublishedJobsAsync(1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Title.Should().Be("Live Job");
    }

    [Theory]
    [InlineData(JobStatus.Deactivated)]
    [InlineData(JobStatus.PendingReview)]
    public async Task GetPublishedJobByIdAsync_ReturnsNullForModeratedJobs(JobStatus status)
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        var job = MakeJob("Moderated Job", status);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repository.GetPublishedJobByIdAsync(job.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEmployerJobsPaginatedAsync_IncludesDeactivatedJobsWithTheReasonLabel()
    {
        await using var context = NewContext();
        var repository = new JobRepository(context);
        context.Jobs.AddRange(
            MakeJob("Live Job", JobStatus.Published),
            MakeJob("Deactivated Job", JobStatus.Deactivated));
        await context.SaveChangesAsync();

        var result = await repository.GetEmployerJobsPaginatedAsync("employer-1", null, null, 1, 20);

        result.Items.Should().HaveCount(2);
        var deactivated = result.Items.Single(item => item.Title == "Deactivated Job");
        deactivated.Status.Should().Be("Deactivated");
        deactivated.DeactivationReasonLabel.Should().Be("Inappropriate content");
        result.Items.Single(item => item.Title == "Live Job").DeactivationReasonLabel.Should().BeNull();
    }
}

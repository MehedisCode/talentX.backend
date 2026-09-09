using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class JobApplicationRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static JobApplication NewApplication(Guid? jobId = null, string? talentUserId = "talent-1")
    {
        return new JobApplication(
            jobId ?? Guid.NewGuid(),
            talentUserId,
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf",
            "Cover letter text",
            9000m,
            4.0m,
            "Remote");
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_PersistsAndRetrievesApplication()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var application = NewApplication();

        await repository.AddAsync(application);
        await context.SaveChangesAsync();

        var retrievedApplication = await repository.GetByIdAsync(application.Id);

        retrievedApplication.Should().NotBeNull();
        retrievedApplication!.Id.Should().Be(application.Id);
        retrievedApplication.ApplicantName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task ExistsByJobIdAndTalentUserIdAsync_ReturnsCorrectStatus()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var targetJobId = Guid.NewGuid();
        var application = NewApplication(jobId: targetJobId, talentUserId: "talent-specific");

        await repository.AddAsync(application);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsByJobIdAndTalentUserIdAsync(targetJobId, "talent-specific");
        var notExistsDifferentTalent = await repository.ExistsByJobIdAndTalentUserIdAsync(targetJobId, "talent-other");
        var notExistsDifferentJob = await repository.ExistsByJobIdAndTalentUserIdAsync(Guid.NewGuid(), "talent-specific");

        exists.Should().BeTrue();
        notExistsDifferentTalent.Should().BeFalse();
        notExistsDifferentJob.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsPendingByJobIdAndEmailAsync_And_GetPendingByJobIdAndEmailAsync_WorkCorrectly()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var targetJobId = Guid.NewGuid();
        var guestApp = JobApplication.CreateGuestApplication(
            targetJobId, "Guest User", "guest@example.com", "resumes/guest.pdf");

        await repository.AddAsync(guestApp);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsPendingByJobIdAndEmailAsync(targetJobId, "guest@example.com");
        var notExistsDifferentEmail = await repository.ExistsPendingByJobIdAndEmailAsync(targetJobId, "other@example.com");
        var pending = await repository.GetPendingByJobIdAndEmailAsync(targetJobId, "guest@example.com");

        exists.Should().BeTrue();
        notExistsDifferentEmail.Should().BeFalse();
        pending.Should().NotBeNull();
        pending!.Id.Should().Be(guestApp.Id);
    }

    [Fact]
    public async Task GetByJobIdAsync_ReturnsApplicationsOnlyForTargetJob()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);
        var targetJobId = Guid.NewGuid();
        var application1 = NewApplication(jobId: targetJobId, talentUserId: "talent-1");
        var application2 = NewApplication(jobId: targetJobId, talentUserId: "talent-2");
        var application3 = NewApplication(jobId: Guid.NewGuid(), talentUserId: "talent-3");

        await repository.AddAsync(application1);
        await repository.AddAsync(application2);
        await repository.AddAsync(application3);
        await context.SaveChangesAsync();

        var applications = await repository.GetByJobIdAsync(targetJobId);

        applications.Should().HaveCount(2);
        applications.Select(app => app.Id).Should().Contain(new[] { application1.Id, application2.Id });
    }

    [Fact]
    public async Task CountHiredByEmployerIdAsync_WhenApplicationsExist_CountsHiredForEmployerIncludingSoftDeletedJobs()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);

        var employerOneJobOne = MakeJob("employer-1");
        var employerOneJobTwo = MakeJob("employer-1");
        var employerOneDeletedJob = MakeJob("employer-1");
        employerOneDeletedJob.SoftDelete();

        var employerTwoJob = MakeJob("employer-2");

        context.Jobs.AddRange(employerOneJobOne, employerOneJobTwo, employerOneDeletedJob, employerTwoJob);

        var hiredApplicationOne = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-1");
        hiredApplicationOne.UpdateStatus(ApplicationStatus.Hired);

        var appliedApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-2");
        appliedApplication.UpdateStatus(ApplicationStatus.Applied);

        var hiredApplicationTwo = NewApplication(jobId: employerOneJobTwo.Id, talentUserId: "talent-3");
        hiredApplicationTwo.UpdateStatus(ApplicationStatus.Hired);

        var hiredApplicationOnDeletedJob = NewApplication(jobId: employerOneDeletedJob.Id, talentUserId: "talent-4");
        hiredApplicationOnDeletedJob.UpdateStatus(ApplicationStatus.Hired);

        var hiredApplicationEmployerTwo = NewApplication(jobId: employerTwoJob.Id, talentUserId: "talent-5");
        hiredApplicationEmployerTwo.UpdateStatus(ApplicationStatus.Hired);

        context.JobApplications.AddRange(
            hiredApplicationOne,
            appliedApplication,
            hiredApplicationTwo,
            hiredApplicationOnDeletedJob,
            hiredApplicationEmployerTwo);
        await context.SaveChangesAsync();

        var employerOneCount = await repository.CountHiredByEmployerIdAsync("employer-1");
        var employerTwoCount = await repository.CountHiredByEmployerIdAsync("employer-2");
        var unknownEmployerCount = await repository.CountHiredByEmployerIdAsync("employer-unknown");

        employerOneCount.Should().Be(3);
        employerTwoCount.Should().Be(1);
        unknownEmployerCount.Should().Be(0);
    }

    private static Job MakeJob(string employerId = "employer-1")
    {
        var job = Job.Create(
            employerId,
            "Software Engineer",
            "Acme Corp",
            "Job description",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.RemoteButAccessible,
            ["C#"],
            1,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }
}

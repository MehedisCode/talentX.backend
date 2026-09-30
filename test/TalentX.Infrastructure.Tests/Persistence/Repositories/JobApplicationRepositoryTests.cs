using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Features.Employers.DTOs;
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

    [Fact]
    public async Task GetCandidatePipelineStatsByEmployerIdAsync_WhenApplicationsExist_ReturnsCorrectStageCountsIncludingSoftDeletedJobs()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);

        var employerOneJobOne = MakeJob("employer-1");
        var employerOneDeletedJob = MakeJob("employer-1");
        employerOneDeletedJob.SoftDelete();

        var employerTwoJob = MakeJob("employer-2");

        context.Jobs.AddRange(employerOneJobOne, employerOneDeletedJob, employerTwoJob);

        var appliedApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-applied");
        appliedApplication.UpdateStatus(ApplicationStatus.Applied);

        var shortlistedApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-shortlisted");
        shortlistedApplication.UpdateStatus(ApplicationStatus.Shortlisted);

        var invitedApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-invited");
        invitedApplication.UpdateStatus(ApplicationStatus.Invited);

        var rejectedApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-rejected");
        rejectedApplication.UpdateStatus(ApplicationStatus.Rejected);

        var hiredApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-hired");
        hiredApplication.UpdateStatus(ApplicationStatus.Hired);

        var hiredOnDeletedJob = NewApplication(jobId: employerOneDeletedJob.Id, talentUserId: "talent-deleted-hired");
        hiredOnDeletedJob.UpdateStatus(ApplicationStatus.Hired);

        var otherStatusApplication = NewApplication(jobId: employerOneJobOne.Id, talentUserId: "talent-reviewed");
        otherStatusApplication.UpdateStatus(ApplicationStatus.Reviewed);

        var employerTwoApplied = NewApplication(jobId: employerTwoJob.Id, talentUserId: "talent-emp2");
        employerTwoApplied.UpdateStatus(ApplicationStatus.Applied);

        context.JobApplications.AddRange(
            appliedApplication,
            shortlistedApplication,
            invitedApplication,
            rejectedApplication,
            hiredApplication,
            hiredOnDeletedJob,
            otherStatusApplication,
            employerTwoApplied);
        await context.SaveChangesAsync();

        var pipelineStats = await repository.GetCandidatePipelineStatsByEmployerIdAsync("employer-1");

        pipelineStats.Should().NotBeNull();
        pipelineStats.AppliedCount.Should().Be(1);
        pipelineStats.ShortlistedCount.Should().Be(1);
        pipelineStats.InvitedCount.Should().Be(1);
        pipelineStats.RejectedCount.Should().Be(1);
        pipelineStats.HiredCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCandidatePipelineStatsByEmployerIdAsync_WhenNoApplicationsExist_ReturnsZeroCounts()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);

        var job = MakeJob("employer-empty");
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var pipelineStats = await repository.GetCandidatePipelineStatsByEmployerIdAsync("employer-empty");

        pipelineStats.Should().NotBeNull();
        pipelineStats.AppliedCount.Should().Be(0);
        pipelineStats.ShortlistedCount.Should().Be(0);
        pipelineStats.InvitedCount.Should().Be(0);
        pipelineStats.RejectedCount.Should().Be(0);
        pipelineStats.HiredCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCandidatePipelineStatsByJobIdAsync_WhenApplicationsExist_ReturnsCorrectStageCounts()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);

        var targetJobId = Guid.NewGuid();
        var otherJobId = Guid.NewGuid();

        var appliedApplicationOne = NewApplication(jobId: targetJobId, talentUserId: "talent-applied-1");
        appliedApplicationOne.UpdateStatus(ApplicationStatus.Applied);

        var appliedApplicationTwo = NewApplication(jobId: targetJobId, talentUserId: "talent-applied-2");
        appliedApplicationTwo.UpdateStatus(ApplicationStatus.Applied);

        var shortlistedApplication = NewApplication(jobId: targetJobId, talentUserId: "talent-shortlisted");
        shortlistedApplication.UpdateStatus(ApplicationStatus.Shortlisted);

        var invitedApplication = NewApplication(jobId: targetJobId, talentUserId: "talent-invited");
        invitedApplication.UpdateStatus(ApplicationStatus.Invited);

        var rejectedApplication = NewApplication(jobId: targetJobId, talentUserId: "talent-rejected");
        rejectedApplication.UpdateStatus(ApplicationStatus.Rejected);

        var hiredApplication = NewApplication(jobId: targetJobId, talentUserId: "talent-hired");
        hiredApplication.UpdateStatus(ApplicationStatus.Hired);

        var otherStatusApplication = NewApplication(jobId: targetJobId, talentUserId: "talent-reviewed");
        otherStatusApplication.UpdateStatus(ApplicationStatus.Reviewed);

        var otherJobApplication = NewApplication(jobId: otherJobId, talentUserId: "talent-other-job");
        otherJobApplication.UpdateStatus(ApplicationStatus.Applied);

        context.JobApplications.AddRange(
            appliedApplicationOne,
            appliedApplicationTwo,
            shortlistedApplication,
            invitedApplication,
            rejectedApplication,
            hiredApplication,
            otherStatusApplication,
            otherJobApplication);
        await context.SaveChangesAsync();

        var candidatePipelineStatistics = await repository.GetCandidatePipelineStatsByJobIdAsync(targetJobId);

        candidatePipelineStatistics.Should().NotBeNull();
        candidatePipelineStatistics.AppliedCount.Should().Be(2);
        candidatePipelineStatistics.ShortlistedCount.Should().Be(1);
        candidatePipelineStatistics.InvitedCount.Should().Be(1);
        candidatePipelineStatistics.RejectedCount.Should().Be(1);
        candidatePipelineStatistics.HiredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCandidatePipelineStatsByJobIdAsync_WhenNoApplicationsExist_ReturnsZeroCounts()
    {
        await using var context = NewContext();
        var repository = new JobApplicationRepository(context);

        var candidatePipelineStatistics = await repository.GetCandidatePipelineStatsByJobIdAsync(Guid.NewGuid());

        candidatePipelineStatistics.Should().NotBeNull();
        candidatePipelineStatistics.AppliedCount.Should().Be(0);
        candidatePipelineStatistics.ShortlistedCount.Should().Be(0);
        candidatePipelineStatistics.InvitedCount.Should().Be(0);
        candidatePipelineStatistics.RejectedCount.Should().Be(0);
        candidatePipelineStatistics.HiredCount.Should().Be(0);
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

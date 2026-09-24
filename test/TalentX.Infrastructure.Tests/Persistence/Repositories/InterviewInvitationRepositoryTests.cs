using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;
using TalentX.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TalentX.Infrastructure.Tests.Persistence.Repositories;

public sealed class InterviewInvitationRepositoryTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static JobApplication NewApplication(Guid jobId)
    {
        return new JobApplication(
            jobId,
            "talent-1",
            "Jane Doe",
            "jane@example.com",
            "resumes/jane.pdf",
            "Cover letter text",
            9000m,
            4.0m,
            "Remote");
    }

    private static InterviewInvitation NewInvitation(Guid jobId, Guid jobApplicationId, InterviewType type = InterviewType.OnlineAssessment)
    {
        return new InterviewInvitation(
            jobId,
            jobApplicationId,
            "employer-1",
            "talent-1",
            type);
    }

    [Fact]
    public async Task GetByJobIdPaginatedAsync_WhenFilteringByStatus_ReturnsOnlyMatchingStatus()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var jobId = Guid.NewGuid();

        var pendingApp = NewApplication(jobId);
        var acceptedApp = NewApplication(jobId);
        var declinedApp = NewApplication(jobId);
        context.JobApplications.AddRange(pendingApp, acceptedApp, declinedApp);

        var pending = NewInvitation(jobId, pendingApp.Id);
        var accepted = NewInvitation(jobId, acceptedApp.Id);
        accepted.Accept(null, null);
        var declined = NewInvitation(jobId, declinedApp.Id);
        declined.Decline();

        context.InterviewInvitations.AddRange(pending, accepted, declined);
        await context.SaveChangesAsync();

        var filter = new InterviewInvitationListFilter(Status: InterviewInvitationStatus.Accepted);
        var result = await repository.GetByJobIdPaginatedAsync(jobId, filter, 1, 10);

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(accepted.Id);
    }

    [Fact]
    public async Task GetByJobIdPaginatedAsync_WhenFilteringByType_ReturnsOnlyMatchingType()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var jobId = Guid.NewGuid();

        var onSiteApp = NewApplication(jobId);
        var onCallApp = NewApplication(jobId);
        context.JobApplications.AddRange(onSiteApp, onCallApp);

        var onSite = NewInvitation(jobId, onSiteApp.Id, InterviewType.OnSite);
        var onCall = NewInvitation(jobId, onCallApp.Id, InterviewType.OnCall);

        context.InterviewInvitations.AddRange(onSite, onCall);
        await context.SaveChangesAsync();

        var filter = new InterviewInvitationListFilter(Type: InterviewType.OnSite);
        var result = await repository.GetByJobIdPaginatedAsync(jobId, filter, 1, 10);

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(onSite.Id);
    }

    [Fact]
    public async Task GetByJobIdPaginatedAsync_WhenFilteringByStatusAndType_ReturnsOnlyMatchingBoth()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var jobId = Guid.NewGuid();

        var matchingApp = NewApplication(jobId);
        var wrongTypeApp = NewApplication(jobId);
        var wrongStatusApp = NewApplication(jobId);
        context.JobApplications.AddRange(matchingApp, wrongTypeApp, wrongStatusApp);

        var matching = NewInvitation(jobId, matchingApp.Id, InterviewType.OnSite);
        matching.Accept(null, null);

        var wrongType = NewInvitation(jobId, wrongTypeApp.Id, InterviewType.OnCall);
        wrongType.Accept(null, null);

        var wrongStatus = NewInvitation(jobId, wrongStatusApp.Id, InterviewType.OnSite);

        context.InterviewInvitations.AddRange(matching, wrongType, wrongStatus);
        await context.SaveChangesAsync();

        var filter = new InterviewInvitationListFilter(
            Status: InterviewInvitationStatus.Accepted,
            Type: InterviewType.OnSite);
        var result = await repository.GetByJobIdPaginatedAsync(jobId, filter, 1, 10);

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(matching.Id);
    }

    [Fact]
    public async Task GetByJobIdPaginatedAsync_WithNoFilter_ReturnsEverythingForTheJob()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var jobId = Guid.NewGuid();
        var otherJobId = Guid.NewGuid();

        var pendingApp = NewApplication(jobId);
        var acceptedApp = NewApplication(jobId);
        var otherJobApp = NewApplication(otherJobId);
        context.JobApplications.AddRange(pendingApp, acceptedApp, otherJobApp);

        var pending = NewInvitation(jobId, pendingApp.Id);
        var accepted = NewInvitation(jobId, acceptedApp.Id);
        accepted.Accept(null, null);
        var otherJob = NewInvitation(otherJobId, otherJobApp.Id);

        context.InterviewInvitations.AddRange(pending, accepted, otherJob);
        await context.SaveChangesAsync();

        var result = await repository.GetByJobIdPaginatedAsync(jobId, new InterviewInvitationListFilter(), 1, 10);

        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Id).Should().Contain(new[] { pending.Id, accepted.Id });
    }

    // ---- GetAcceptedBy... (company reviews eligibility) ----

    private static Job NewJob(bool deleted = false)
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build things.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));
        if (deleted)
        {
            job.SoftDelete();
        }

        return job;
    }

    private static InterviewInvitation NewAccepted(Guid jobId, Guid applicationId, string talentUserId = "talent-1")
    {
        var invitation = new InterviewInvitation(
            jobId, applicationId, "employer-1", talentUserId, InterviewType.OnCall);
        invitation.AddTimeslot(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(1));
        invitation.Accept(invitation.Timeslots[0].Id, invitation.Timeslots[0].StartTime);
        return invitation;
    }

    [Fact]
    public async Task GetAcceptedByTalentUserIdAsync_ReturnsOnlyAcceptedInvitations()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var acceptedApp = NewApplication(job.Id);
        var pendingApp = NewApplication(job.Id);
        var declinedApp = NewApplication(job.Id);
        var cancelledApp = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.AddRange(acceptedApp, pendingApp, declinedApp, cancelledApp);

        var accepted = NewAccepted(job.Id, acceptedApp.Id);
        var pending = NewInvitation(job.Id, pendingApp.Id);
        var declined = NewInvitation(job.Id, declinedApp.Id);
        declined.Decline();
        var cancelled = NewAccepted(job.Id, cancelledApp.Id);
        cancelled.Cancel();
        context.InterviewInvitations.AddRange(accepted, pending, declined, cancelled);
        await context.SaveChangesAsync();

        var result = await repository.GetAcceptedByTalentUserIdAsync("talent-1");

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { accepted.Id });
    }

    [Fact]
    public async Task GetAcceptedByTalentUserIdAsync_DoesNotReturnOtherTalentsInvitations()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var mineApp = NewApplication(job.Id);
        var theirsApp = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.AddRange(mineApp, theirsApp);
        var mine = NewAccepted(job.Id, mineApp.Id, "talent-1");
        var theirs = NewAccepted(job.Id, theirsApp.Id, "talent-2");
        context.InterviewInvitations.AddRange(mine, theirs);
        await context.SaveChangesAsync();

        var result = await repository.GetAcceptedByTalentUserIdAsync("talent-1");

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { mine.Id });
    }

    [Fact]
    public async Task GetAcceptedByTalentUserIdAsync_StillReturnsInvitationWhoseJobWasSoftDeleted()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob(deleted: true);
        var app = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.Add(app);
        var accepted = NewAccepted(job.Id, app.Id);
        context.InterviewInvitations.Add(accepted);
        await context.SaveChangesAsync();

        var result = await repository.GetAcceptedByTalentUserIdAsync("talent-1");

        result.Should().ContainSingle();
        result.Single().Job.Should().NotBeNull();
        result.Single().Job!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAcceptedByTalentUserIdAsync_LoadsTimeslotsJobAndApplication()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var app = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.Add(app);
        var accepted = NewAccepted(job.Id, app.Id);
        context.InterviewInvitations.Add(accepted);
        await context.SaveChangesAsync();

        var result = (await repository.GetAcceptedByTalentUserIdAsync("talent-1")).Single();

        result.Timeslots.Should().ContainSingle();
        result.SelectedTimeslotId.Should().Be(result.Timeslots[0].Id);
        result.Job!.Title.Should().Be("Backend Engineer");
        result.JobApplication!.Id.Should().Be(app.Id);
    }

    [Fact]
    public async Task GetAcceptedByApplicationIdAsync_ReturnsEveryAcceptedRoundOfThatApplicationOnly()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var app = NewApplication(job.Id);
        var otherApp = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.AddRange(app, otherApp);

        var round1 = NewAccepted(job.Id, app.Id);
        var round2 = NewAccepted(job.Id, app.Id);
        var declinedRound = NewInvitation(job.Id, app.Id);
        declinedRound.Decline();
        var otherAppRound = NewAccepted(job.Id, otherApp.Id);
        context.InterviewInvitations.AddRange(round1, round2, declinedRound, otherAppRound);
        await context.SaveChangesAsync();

        var result = await repository.GetAcceptedByApplicationIdAsync(app.Id);

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { round1.Id, round2.Id });
        result.Should().OnlyContain(x => x.Timeslots.Count == 1);
    }

    [Fact]
    public async Task GetAcceptedByApplicationIdAsync_NoAcceptedRounds_ReturnsEmpty()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var app = NewApplication(job.Id);
        context.Jobs.Add(job);
        context.JobApplications.Add(app);
        context.InterviewInvitations.Add(NewInvitation(job.Id, app.Id));
        await context.SaveChangesAsync();

        var result = await repository.GetAcceptedByApplicationIdAsync(app.Id);

        result.Should().BeEmpty();
    }
}

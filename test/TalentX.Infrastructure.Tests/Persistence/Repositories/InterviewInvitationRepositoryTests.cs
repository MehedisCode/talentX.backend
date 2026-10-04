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

    // ---- GetByTalentUserIdPaginatedAsync (talent "My Interviews" ordering) ----

    [Fact]
    public async Task GetByTalentUserIdPaginatedAsync_OrdersUpcomingFirstThenEndedThenDeclined()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var jobId = job.Id;
        context.Jobs.Add(job);
        var now = DateTime.UtcNow;

        var soonUpcomingApp = NewApplication(jobId);
        var laterUpcomingApp = NewApplication(jobId);
        var recentlyEndedApp = NewApplication(jobId);
        var longAgoExpiredApp = NewApplication(jobId);
        var cancelledApp = NewApplication(jobId);
        var declinedApp = NewApplication(jobId);
        context.JobApplications.AddRange(
            soonUpcomingApp, laterUpcomingApp, recentlyEndedApp, longAgoExpiredApp, cancelledApp, declinedApp);

        // Bucket 0 (upcoming): accepted, interview still ahead — nearer one must sort first.
        var soonUpcoming = NewInvitation(jobId, soonUpcomingApp.Id, InterviewType.OnCall);
        soonUpcoming.AddTimeslot(now.AddDays(1), now.AddDays(1).AddHours(1));
        soonUpcoming.Accept(soonUpcoming.Timeslots[0].Id, soonUpcoming.Timeslots[0].StartTime);

        var laterUpcoming = NewInvitation(jobId, laterUpcomingApp.Id, InterviewType.OnCall);
        laterUpcoming.AddTimeslot(now.AddDays(5), now.AddDays(5).AddHours(1));
        laterUpcoming.Accept(laterUpcoming.Timeslots[0].Id, laterUpcoming.Timeslots[0].StartTime);

        // Bucket 1 (ended/cancelled): accepted-but-over, never-answered, and cancelled — most
        // recently ended first.
        var recentlyEnded = NewInvitation(jobId, recentlyEndedApp.Id, InterviewType.OnCall);
        recentlyEnded.AddTimeslot(now.AddDays(-1), now.AddDays(-1).AddHours(1));
        recentlyEnded.Accept(recentlyEnded.Timeslots[0].Id, recentlyEnded.Timeslots[0].StartTime);

        var longAgoExpired = new InterviewInvitation(
            jobId,
            longAgoExpiredApp.Id,
            "employer-1",
            "talent-1",
            InterviewType.OnlineAssessment,
            assessmentUrl: "https://assessment.example.com",
            assessmentDeadline: now.AddDays(-10));

        var cancelled = NewInvitation(jobId, cancelledApp.Id, InterviewType.OnCall);
        cancelled.AddTimeslot(now.AddDays(-3), now.AddDays(-3).AddHours(1));
        cancelled.Cancel();

        // Bucket 2 (declined): always last.
        var declined = NewInvitation(jobId, declinedApp.Id);
        declined.Decline();

        context.InterviewInvitations.AddRange(
            laterUpcoming, declined, cancelled, longAgoExpired, soonUpcoming, recentlyEnded);
        await context.SaveChangesAsync();

        var result = await repository.GetByTalentUserIdPaginatedAsync("talent-1", 1, 10);

        result.Items.Select(x => x.Id).Should().ContainInOrder(
            soonUpcoming.Id,
            laterUpcoming.Id,
            recentlyEnded.Id,
            cancelled.Id,
            longAgoExpired.Id,
            declined.Id);
    }

    [Fact]
    public async Task GetByTalentUserIdPaginatedAsync_PaginatesAfterSorting()
    {
        await using var context = NewContext();
        var repository = new InterviewInvitationRepository(context);
        var job = NewJob();
        var jobId = job.Id;
        context.Jobs.Add(job);
        var now = DateTime.UtcNow;

        var nearApp = NewApplication(jobId);
        var farApp = NewApplication(jobId);
        context.JobApplications.AddRange(nearApp, farApp);

        var near = NewInvitation(jobId, nearApp.Id, InterviewType.OnCall);
        near.AddTimeslot(now.AddDays(1), now.AddDays(1).AddHours(1));
        near.Accept(near.Timeslots[0].Id, near.Timeslots[0].StartTime);

        var far = NewInvitation(jobId, farApp.Id, InterviewType.OnCall);
        far.AddTimeslot(now.AddDays(10), now.AddDays(10).AddHours(1));
        far.Accept(far.Timeslots[0].Id, far.Timeslots[0].StartTime);

        context.InterviewInvitations.AddRange(far, near);
        await context.SaveChangesAsync();

        var firstPage = await repository.GetByTalentUserIdPaginatedAsync("talent-1", 1, 1);
        var secondPage = await repository.GetByTalentUserIdPaginatedAsync("talent-1", 2, 1);

        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Single().Id.Should().Be(near.Id);
        secondPage.Items.Single().Id.Should().Be(far.Id);
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

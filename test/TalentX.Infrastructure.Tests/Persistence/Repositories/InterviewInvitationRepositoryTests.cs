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
}

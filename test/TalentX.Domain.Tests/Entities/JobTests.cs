using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

// Job is a pure data entity now — normalization (trim/dedupe) and business
// rules (publishability) live in TalentX.Application.Features.Jobs.JobRules
// instead. These tests only check that the entity stores what it's given.
public class JobTests
{
    private static Job CreateJob(
        string employerId = "employer-1",
        string title = "Backend Engineer",
        string companyName = "Acme Corp",
        string description = "Build and maintain backend services.",
        EmploymentType employmentType = EmploymentType.FullTime,
        ExperienceLevel experienceLevel = ExperienceLevel.MidLevel,
        string location = "Dhaka",
        WorkArrangement workArrangement = WorkArrangement.FullRemote,
        List<string>? requiredSkills = null,
        int vacancies = 2,
        DateTime? expireAt = null
    )
    {
        return Job.Create(
            employerId,
            title,
            companyName,
            description,
            employmentType,
            experienceLevel,
            location,
            workArrangement,
            requiredSkills ?? new List<string> { "C#", "SQL" },
            vacancies,
            expireAt ?? DateTime.UtcNow.AddDays(30)
        );
    }

    [Fact]
    public void Create_SetsAllFieldsFromArguments()
    {
        var skills = new List<string> { "C#", "SQL" };
        var expireAt = DateTime.UtcNow.AddDays(10);

        var job = CreateJob(
            employerId: "employer-42",
            title: "Backend Engineer",
            companyName: "Acme Corp",
            description: "Build things.",
            employmentType: EmploymentType.PartTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            location: "Dhaka",
            workArrangement: WorkArrangement.OnSite,
            requiredSkills: skills,
            vacancies: 5,
            expireAt: expireAt
        );

        job.EmployerId.Should().Be("employer-42");
        job.Title.Should().Be("Backend Engineer");
        job.CompanyName.Should().Be("Acme Corp");
        job.Description.Should().Be("Build things.");
        job.EmploymentType.Should().Be(EmploymentType.PartTime);
        job.ExperienceLevel.Should().Be(ExperienceLevel.SeniorLevel);
        job.Location.Should().Be("Dhaka");
        job.WorkArrangement.Should().Be(WorkArrangement.OnSite);
        job.RequiredSkills.Should().BeEquivalentTo(skills);
        job.Vacancies.Should().Be(5);
        job.ExpireAt.Should().Be(expireAt);
    }

    [Fact]
    public void Create_DoesNotTransformInput()
    {
        // Trimming/deduping is JobRules' job now, not the entity's — the
        // entity stores exactly what it's handed.
        var job = CreateJob(
            title: "  Backend Engineer  ",
            requiredSkills: new List<string> { " C# ", "c#" }
        );

        job.Title.Should().Be("  Backend Engineer  ");
        job.RequiredSkills.Should().BeEquivalentTo(new[] { " C# ", "c#" });
    }

    [Fact]
    public void Create_SetsStatusToDraft()
    {
        var job = CreateJob();

        job.Status.Should().Be(JobStatus.Draft);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var job = CreateJob();
        var after = DateTime.UtcNow;

        job.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_LeavesUpdatedAtNull()
    {
        var job = CreateJob();

        job.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_LeavesPublishedAtNull()
    {
        var job = CreateJob();

        job.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void SetDetails_ReplacesAllFields()
    {
        var job = CreateJob(title: "Old Title", vacancies: 1);
        var newSkills = new List<string> { "Go" };
        var newExpireAt = DateTime.UtcNow.AddDays(60);

        job.SetDetails(
            "New Title", "New Co", "New Desc",
            EmploymentType.Contract, ExperienceLevel.EntryLevel, "New Loc",
            WorkArrangement.Hybrid, newSkills, 9, newExpireAt
        );

        job.Title.Should().Be("New Title");
        job.CompanyName.Should().Be("New Co");
        job.Description.Should().Be("New Desc");
        job.EmploymentType.Should().Be(EmploymentType.Contract);
        job.ExperienceLevel.Should().Be(ExperienceLevel.EntryLevel);
        job.Location.Should().Be("New Loc");
        job.WorkArrangement.Should().Be(WorkArrangement.Hybrid);
        job.RequiredSkills.Should().BeEquivalentTo(newSkills);
        job.Vacancies.Should().Be(9);
        job.ExpireAt.Should().Be(newExpireAt);
    }

    [Fact]
    public void SetDetails_SetsUpdatedAtToUtcNow()
    {
        var job = CreateJob();

        var before = DateTime.UtcNow;
        job.SetDetails(
            "New Title", job.CompanyName, job.Description, job.EmploymentType,
            job.ExperienceLevel, job.Location, job.WorkArrangement, job.RequiredSkills,
            job.Vacancies, job.ExpireAt
        );
        var after = DateTime.UtcNow;

        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsPublished_SetsStatusToPublished()
    {
        var job = CreateJob();

        job.MarkAsPublished();

        job.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public void MarkAsPublished_SetsPublishedAtToUtcNow()
    {
        var job = CreateJob();
        var before = DateTime.UtcNow;

        job.MarkAsPublished();

        var after = DateTime.UtcNow;
        job.PublishedAt.Should().NotBeNull();
        job.PublishedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsPublished_DoesNotValidateCompleteness()
    {
        // No guardrail here by design — JobRules.EnsurePublishable is what
        // callers are expected to run first. The entity just flips status.
        var job = CreateJob(title: "");

        var act = () => job.MarkAsPublished();

        act.Should().NotThrow();
        job.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public void UpdateWizardDetails_SetsWizardPropertiesAndUpdatesTimestamp()
    {
        var job = CreateJob();
        var beforeUpdate = DateTime.UtcNow;

        job.UpdateWizardDetails(
            city: "Berlin",
            country: "Germany",
            minOverlapHours: 4,
            responsibilities: "Lead architectural designs and mentor junior engineers."
        );

        var afterUpdate = DateTime.UtcNow;

        job.City.Should().Be("Berlin");
        job.Country.Should().Be("Germany");
        job.MinOverlapHours.Should().Be(4);
        job.Responsibilities.Should().Be("Lead architectural designs and mentor junior engineers.");
        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void MarkAsClosed_SetsStatusToClosedAndUpdatesTimestamp()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        var beforeClosed = DateTime.UtcNow;

        job.MarkAsClosed();

        var afterClosed = DateTime.UtcNow;

        job.Status.Should().Be(JobStatus.Closed);
        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeClosed).And.BeOnOrBefore(afterClosed);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAtAndUpdatesTimestamp()
    {
        var job = CreateJob();
        var beforeDeleted = DateTime.UtcNow;

        job.SoftDelete();

        var afterDeleted = DateTime.UtcNow;

        job.IsDeleted.Should().BeTrue();
        job.DeletedAt.Should().NotBeNull();
        job.DeletedAt!.Value.Should().BeOnOrAfter(beforeDeleted).And.BeOnOrBefore(afterDeleted);
        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeDeleted).And.BeOnOrBefore(afterDeleted);
    }

    [Fact]
    public void Reopen_WhenClosedAndFutureExpiry_SetsStatusPublished()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        job.MarkAsClosed();
        var beforeReopen = DateTime.UtcNow;

        job.Reopen();

        var afterReopen = DateTime.UtcNow;
        job.Status.Should().Be(JobStatus.Published);
        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeReopen).And.BeOnOrBefore(afterReopen);
    }

    [Fact]
    public void Reopen_BumpsPublishedAtToUtcNow()
    {
        // Reopening relists the job — it should read as freshly posted again,
        // not still ranked by whenever it was first published.
        var job = CreateJob();
        job.MarkAsPublished();
        var originalPublishedAt = job.PublishedAt;
        Thread.Sleep(5);
        job.MarkAsClosed();
        var beforeReopen = DateTime.UtcNow;

        job.Reopen();

        var afterReopen = DateTime.UtcNow;
        job.PublishedAt.Should().NotBeNull();
        job.PublishedAt!.Value.Should().BeOnOrAfter(beforeReopen).And.BeOnOrBefore(afterReopen);
        job.PublishedAt.Should().BeAfter(originalPublishedAt!.Value);
    }

    [Fact]
    public void Reopen_WhenNotClosed_ThrowsInvalidOperationException()
    {
        var job = CreateJob();

        var reopenAction = () => job.Reopen();

        reopenAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Only closed jobs can be reopened.");
    }

    [Fact]
    public void Reopen_WhenExpiredDeadline_ThrowsInvalidOperationException()
    {
        var expiredDate = DateTime.UtcNow.AddDays(-2);
        var job = CreateJob(expireAt: expiredDate);
        job.MarkAsPublished();
        job.MarkAsClosed();

        var reopenAction = () => job.Reopen();

        reopenAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reopen a job with an expired deadline. Update the application deadline before reopening.");
    }

    [Theory]
    [InlineData(JobStatus.Draft)]
    [InlineData(JobStatus.Closed)]
    [InlineData(JobStatus.Deactivated)]
    [InlineData(JobStatus.PendingReview)]
    public void MarkAsClosed_WhenNotPublished_ThrowsInvalidOperationException(JobStatus status)
    {
        var job = CreateJobWithStatus(status);

        var closeAction = () => job.MarkAsClosed();

        closeAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Only published jobs can be closed.");
        job.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(JobStatus.Published)]
    [InlineData(JobStatus.PendingReview)]
    public void Deactivate_WhenPublishedOrPendingReview_SetsDeactivatedStateAndReason(JobStatus status)
    {
        var job = CreateJobWithStatus(status);
        var beforeDeactivate = DateTime.UtcNow;

        job.Deactivate(DeactivationReason.Spam, "Posting links to an unrelated external site.");

        var afterDeactivate = DateTime.UtcNow;
        job.Status.Should().Be(JobStatus.Deactivated);
        job.DeactivationReason.Should().Be(DeactivationReason.Spam);
        job.DeactivationDetails.Should().Be("Posting links to an unrelated external site.");
        job.DeactivatedAt.Should().NotBeNull();
        job.DeactivatedAt!.Value.Should().BeOnOrAfter(beforeDeactivate).And.BeOnOrBefore(afterDeactivate);
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeDeactivate).And.BeOnOrBefore(afterDeactivate);
    }

    [Fact]
    public void Deactivate_WhenPendingReview_ReplacesPreviousReason()
    {
        var job = CreateJobWithStatus(JobStatus.PendingReview);

        job.Deactivate(DeactivationReason.MisleadingInformation, "Salary still does not match the description.");

        job.DeactivationReason.Should().Be(DeactivationReason.MisleadingInformation);
        job.DeactivationDetails.Should().Be("Salary still does not match the description.");
    }

    [Theory]
    [InlineData(JobStatus.Draft)]
    [InlineData(JobStatus.Closed)]
    [InlineData(JobStatus.Deactivated)]
    public void Deactivate_WhenNotPublishedOrPendingReview_ThrowsInvalidOperationException(JobStatus status)
    {
        var job = CreateJobWithStatus(status);

        var deactivateAction = () => job.Deactivate(DeactivationReason.Other, "Some explanation text.");

        deactivateAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Only published or under-review jobs can be deactivated.");
        job.Status.Should().Be(status);
    }

    [Fact]
    public void SubmitForReview_WhenDeactivated_SetsPendingReviewAndKeepsReason()
    {
        var job = CreateJobWithStatus(JobStatus.Deactivated);
        var beforeSubmit = DateTime.UtcNow;

        job.SubmitForReview();

        var afterSubmit = DateTime.UtcNow;
        job.Status.Should().Be(JobStatus.PendingReview);
        job.DeactivationReason.Should().Be(DeactivationReason.Spam);
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeSubmit).And.BeOnOrBefore(afterSubmit);
    }

    [Theory]
    [InlineData(JobStatus.Draft)]
    [InlineData(JobStatus.Published)]
    [InlineData(JobStatus.Closed)]
    [InlineData(JobStatus.PendingReview)]
    public void SubmitForReview_WhenNotDeactivated_ThrowsInvalidOperationException(JobStatus status)
    {
        var job = CreateJobWithStatus(status);

        var submitAction = () => job.SubmitForReview();

        submitAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Only deactivated jobs can be resubmitted for review.");
        job.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(JobStatus.Deactivated)]
    [InlineData(JobStatus.PendingReview)]
    public void Reactivate_WhenDeactivatedOrPendingReview_PublishesAndClearsReason(JobStatus status)
    {
        var job = CreateJobWithStatus(status);
        var beforeReactivate = DateTime.UtcNow;

        job.Reactivate();

        var afterReactivate = DateTime.UtcNow;
        job.Status.Should().Be(JobStatus.Published);
        job.DeactivationReason.Should().BeNull();
        job.DeactivationDetails.Should().BeNull();
        job.DeactivatedAt.Should().BeNull();
        job.PublishedAt!.Value.Should().BeOnOrAfter(beforeReactivate).And.BeOnOrBefore(afterReactivate);
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeReactivate).And.BeOnOrBefore(afterReactivate);
    }

    [Theory]
    [InlineData(JobStatus.Draft)]
    [InlineData(JobStatus.Published)]
    [InlineData(JobStatus.Closed)]
    public void Reactivate_WhenNotDeactivatedOrPendingReview_ThrowsInvalidOperationException(JobStatus status)
    {
        var job = CreateJobWithStatus(status);

        var reactivateAction = () => job.Reactivate();

        reactivateAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Only deactivated or under-review jobs can be reactivated.");
        job.Status.Should().Be(status);
    }

    [Fact]
    public void Reactivate_WhenExpiredDeadline_ThrowsAndStaysDeactivated()
    {
        var job = CreateJobWithStatus(JobStatus.Deactivated, expireAt: DateTime.UtcNow.AddDays(-2));

        var reactivateAction = () => job.Reactivate();

        reactivateAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reactivate a job with an expired deadline. The employer must update the application deadline first.");
        job.Status.Should().Be(JobStatus.Deactivated);
        job.DeactivationReason.Should().Be(DeactivationReason.Spam);
    }

    [Fact]
    public void JobModerationHistory_Create_SetsAllFields()
    {
        var jobId = Guid.NewGuid();
        var beforeCreate = DateTime.UtcNow;

        var history = JobModerationHistory.Create(
            jobId,
            JobModerationAction.Deactivated,
            JobStatus.Published,
            JobStatus.Deactivated,
            "admin-1",
            "admin@talentx.com",
            DeactivationReason.DuplicatePosting,
            "Same job posted three times.");

        var afterCreate = DateTime.UtcNow;
        history.JobId.Should().Be(jobId);
        history.Action.Should().Be(JobModerationAction.Deactivated);
        history.FromStatus.Should().Be(JobStatus.Published);
        history.ToStatus.Should().Be(JobStatus.Deactivated);
        history.ActorUserId.Should().Be("admin-1");
        history.ActorEmail.Should().Be("admin@talentx.com");
        history.Reason.Should().Be(DeactivationReason.DuplicatePosting);
        history.Details.Should().Be("Same job posted three times.");
        history.CreatedAt.Should().BeOnOrAfter(beforeCreate).And.BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void JobModerationHistory_Create_WithoutReason_LeavesReasonAndDetailsNull()
    {
        var history = JobModerationHistory.Create(
            Guid.NewGuid(),
            JobModerationAction.Reactivated,
            JobStatus.PendingReview,
            JobStatus.Published,
            "admin-1",
            "admin@talentx.com");

        history.Reason.Should().BeNull();
        history.Details.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenPublishedWithFutureExpiry_ReturnsFalse()
    {
        var job = CreateJobWithStatus(JobStatus.Published, expireAt: DateTime.UtcNow.AddDays(5));

        job.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveStatus_WhenPublishedWithFutureExpiry_ReturnsPublished()
    {
        var job = CreateJobWithStatus(JobStatus.Published, expireAt: DateTime.UtcNow.AddDays(5));

        job.GetEffectiveStatus().Should().Be(JobStatus.Published);
    }

    [Fact]
    public void IsExpired_WhenPublishedWithPastExpiry_ReturnsTrue()
    {
        var job = CreateJobWithStatus(JobStatus.Published, expireAt: DateTime.UtcNow.AddDays(-2));

        job.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveStatus_WhenPublishedWithPastExpiry_ReturnsExpired()
    {
        var job = CreateJobWithStatus(JobStatus.Published, expireAt: DateTime.UtcNow.AddDays(-2));

        job.GetEffectiveStatus().Should().Be(JobStatus.Expired);
    }

    [Fact]
    public void IsExpired_WhenClosedWithPastExpiry_ReturnsFalse()
    {
        var job = CreateJobWithStatus(JobStatus.Closed, expireAt: DateTime.UtcNow.AddDays(-2));

        job.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveStatus_WhenClosedWithPastExpiry_ReturnsClosed()
    {
        var job = CreateJobWithStatus(JobStatus.Closed, expireAt: DateTime.UtcNow.AddDays(-2));

        job.GetEffectiveStatus().Should().Be(JobStatus.Closed);
    }

    [Fact]
    public void IsExpired_WhenDraft_ReturnsFalse()
    {
        var job = CreateJobWithStatus(JobStatus.Draft, expireAt: DateTime.UtcNow.AddDays(-2));

        job.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveStatus_WhenDraft_ReturnsDraft()
    {
        var job = CreateJobWithStatus(JobStatus.Draft, expireAt: DateTime.UtcNow.AddDays(-2));

        job.GetEffectiveStatus().Should().Be(JobStatus.Draft);
    }

    private static Job CreateJobWithStatus(JobStatus status, DateTime? expireAt = null)
    {
        var job = CreateJob(expireAt: expireAt);

        switch (status)
        {
            case JobStatus.Draft:
                break;
            case JobStatus.Published:
                job.MarkAsPublished();
                break;
            case JobStatus.Closed:
                job.MarkAsPublished();
                job.MarkAsClosed();
                break;
            case JobStatus.Deactivated:
                job.MarkAsPublished();
                job.Deactivate(DeactivationReason.Spam, "Initial deactivation reason details.");
                break;
            case JobStatus.PendingReview:
                job.MarkAsPublished();
                job.Deactivate(DeactivationReason.Spam, "Initial deactivation reason details.");
                job.SubmitForReview();
                break;
        }

        return job;
    }
}

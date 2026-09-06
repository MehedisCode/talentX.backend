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
        job.MarkAsClosed();
        var beforeReopen = DateTime.UtcNow;

        job.Reopen();

        var afterReopen = DateTime.UtcNow;
        job.Status.Should().Be(JobStatus.Published);
        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(beforeReopen).And.BeOnOrBefore(afterReopen);
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
        job.MarkAsClosed();

        var reopenAction = () => job.Reopen();

        reopenAction.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reopen a job with an expired deadline. Update the application deadline before reopening.");
    }
}

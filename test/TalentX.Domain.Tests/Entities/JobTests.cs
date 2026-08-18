using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Tests.Domain.Entities;

public class JobTests
{
    private static Job CreateValidDraft(
        string title = "Backend Engineer",
        string companyName = "Acme Corp",
        string description = "Build and maintain backend services.",
        EmploymentType employmentType = EmploymentType.FullTime,
        ExperienceLevel experienceLevel = ExperienceLevel.MidLevel,
        string location = "Dhaka",
        WorkArrangement workArrangement = WorkArrangement.Remote,
        List<string>? requiredSkills = null,
        int vacancies = 2,
        DateTime? expireAt = null
    )
    {
        return Job.CreateDraft(
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
    public void CreateDraft_TrimsStringFields()
    {
        var job = CreateValidDraft(
            title: "  Backend Engineer  ",
            companyName: "  Acme Corp  ",
            description: "  Build things.  ",
            location: "  Dhaka  "
        );

        job.Title.Should().Be("Backend Engineer");
        job.CompanyName.Should().Be("Acme Corp");
        job.Description.Should().Be("Build things.");
        job.Location.Should().Be("Dhaka");
    }

    [Fact]
    public void CreateDraft_SetsStatusToDraft()
    {
        var job = CreateValidDraft();

        job.Status.Should().Be(JobStatus.Draft);
    }

    [Fact]
    public void CreateDraft_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var job = CreateValidDraft();
        var after = DateTime.UtcNow;

        job.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void CreateDraft_LeavesUpdatedAtNull()
    {
        var job = CreateValidDraft();

        job.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void CreateDraft_DoesNotAssignAnIdUpFront()
    {
        // Job's Id is a Guid primary key, so EF Core's client-side value
        // generator assigns it only once the entity is tracked via Add —
        // CreateDraft() alone (no persistence involved) leaves it unset.
        var job = CreateValidDraft();

        job.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void CreateDraft_TrimsAndDeduplicatesRequiredSkills_CaseInsensitively()
    {
        var job = CreateValidDraft(requiredSkills: new List<string> { " C# ", "c#", "SQL", "  " });

        job.RequiredSkills.Should().BeEquivalentTo(new[] { "C#", "SQL" });
    }

    [Fact]
    public void CreateDraft_WithNullRequiredSkills_ResultsInEmptyList()
    {
        var job = Job.CreateDraft(
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Remote,
            null!,
            2,
            DateTime.UtcNow.AddDays(30)
        );

        job.RequiredSkills.Should().BeEmpty();
    }

    [Fact]
    public void CreateDraft_SetsAllScalarFieldsFromArguments()
    {
        var expireAt = DateTime.UtcNow.AddDays(10);
        var job = CreateValidDraft(
            employmentType: EmploymentType.PartTime,
            experienceLevel: ExperienceLevel.SeniorLevel,
            workArrangement: WorkArrangement.OnSite,
            vacancies: 5,
            expireAt: expireAt
        );

        job.EmploymentType.Should().Be(EmploymentType.PartTime);
        job.ExperienceLevel.Should().Be(ExperienceLevel.SeniorLevel);
        job.WorkArrangement.Should().Be(WorkArrangement.OnSite);
        job.Vacancies.Should().Be(5);
        job.ExpireAt.Should().Be(expireAt);
    }

    [Fact]
    public void UpdateDetails_WithAllNullArguments_ChangesNothingExceptUpdatedAt()
    {
        var job = CreateValidDraft();
        var originalTitle = job.Title;
        var originalCompanyName = job.CompanyName;
        var originalDescription = job.Description;
        var originalEmploymentType = job.EmploymentType;
        var originalExperienceLevel = job.ExperienceLevel;
        var originalLocation = job.Location;
        var originalWorkArrangement = job.WorkArrangement;
        var originalSkills = job.RequiredSkills.ToList();
        var originalVacancies = job.Vacancies;
        var originalExpireAt = job.ExpireAt;

        job.UpdateDetails(null, null, null, null, null, null, null, null, null, null);

        job.Title.Should().Be(originalTitle);
        job.CompanyName.Should().Be(originalCompanyName);
        job.Description.Should().Be(originalDescription);
        job.EmploymentType.Should().Be(originalEmploymentType);
        job.ExperienceLevel.Should().Be(originalExperienceLevel);
        job.Location.Should().Be(originalLocation);
        job.WorkArrangement.Should().Be(originalWorkArrangement);
        job.RequiredSkills.Should().BeEquivalentTo(originalSkills);
        job.Vacancies.Should().Be(originalVacancies);
        job.ExpireAt.Should().Be(originalExpireAt);
        job.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDetails_UpdatesOnlyProvidedFields()
    {
        var job = CreateValidDraft(title: "Old Title", vacancies: 1);

        job.UpdateDetails("New Title", null, null, null, null, null, null, null, null, null);

        job.Title.Should().Be("New Title");
        job.Vacancies.Should().Be(1);
    }

    [Fact]
    public void UpdateDetails_TrimsUpdatedStringFields()
    {
        var job = CreateValidDraft();

        job.UpdateDetails("  New Title  ", "  New Co  ", "  New Desc  ", null, null, "  New Loc  ", null, null, null, null);

        job.Title.Should().Be("New Title");
        job.CompanyName.Should().Be("New Co");
        job.Description.Should().Be("New Desc");
        job.Location.Should().Be("New Loc");
    }

    [Fact]
    public void UpdateDetails_ReplacesRequiredSkills_TrimmedAndDeduplicated()
    {
        var job = CreateValidDraft(requiredSkills: new List<string> { "C#" });

        job.UpdateDetails(null, null, null, null, null, null, null, new List<string> { " Go ", "go", "  " }, null, null);

        job.RequiredSkills.Should().BeEquivalentTo(new[] { "Go" });
    }

    [Fact]
    public void UpdateDetails_UpdatesEnumFieldsWhenProvided()
    {
        var job = CreateValidDraft(
            employmentType: EmploymentType.FullTime,
            experienceLevel: ExperienceLevel.MidLevel,
            workArrangement: WorkArrangement.Remote
        );

        job.UpdateDetails(
            null, null, null,
            EmploymentType.Contract,
            ExperienceLevel.EntryLevel,
            null,
            WorkArrangement.Hybrid,
            null, null, null
        );

        job.EmploymentType.Should().Be(EmploymentType.Contract);
        job.ExperienceLevel.Should().Be(ExperienceLevel.EntryLevel);
        job.WorkArrangement.Should().Be(WorkArrangement.Hybrid);
    }

    [Fact]
    public void UpdateDetails_UpdatesVacanciesAndExpireAtWhenProvided()
    {
        var job = CreateValidDraft(vacancies: 1);
        var newExpireAt = DateTime.UtcNow.AddDays(60);

        job.UpdateDetails(null, null, null, null, null, null, null, null, 9, newExpireAt);

        job.Vacancies.Should().Be(9);
        job.ExpireAt.Should().Be(newExpireAt);
    }

    [Fact]
    public void UpdateDetails_SetsUpdatedAtToUtcNow()
    {
        var job = CreateValidDraft();

        var before = DateTime.UtcNow;
        job.UpdateDetails("New Title", null, null, null, null, null, null, null, null, null);
        var after = DateTime.UtcNow;

        job.UpdatedAt.Should().NotBeNull();
        job.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Publish_OnAFullyValidDraft_SetsStatusToPublished()
    {
        var job = CreateValidDraft();

        job.Publish();

        job.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public void EnsureIsPublishable_WithUndefinedEmploymentType_ThrowsAndListsEmploymentType()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, (EmploymentType)999, null, null, null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*EmploymentType*");
    }

    [Fact]
    public void EnsureIsPublishable_WithUndefinedExperienceLevel_ThrowsAndListsExperienceLevel()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, null, (ExperienceLevel)999, null, null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*ExperienceLevel*");
    }

    [Fact]
    public void EnsureIsPublishable_WithUndefinedWorkArrangement_ThrowsAndListsWorkArrangement()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, null, null, null, (WorkArrangement)999, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*WorkArrangement*");
    }

    [Fact]
    public void EmployerId_DefaultsToEmptyGuid_UntilAuthenticationIsWiredUp()
    {
        // TODO in Job.cs: EmployerId isn't populated from the authenticated
        // employer yet, so every draft currently starts with an empty Guid.
        var job = CreateValidDraft();

        job.EmployerId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Publish_WhenNotDraft_ThrowsInvalidOperationException()
    {
        var job = CreateValidDraft();
        job.Publish();

        var act = () => job.Publish();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft jobs can be published.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureIsPublishable_WithBlankTitle_ThrowsAndListsTitle(string title)
    {
        var job = CreateValidDraft();
        job.UpdateDetails(title, null, null, null, null, null, null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Title*");
    }

    [Fact]
    public void EnsureIsPublishable_WithBlankCompanyName_ThrowsAndListsCompanyName()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, "   ", null, null, null, null, null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*CompanyName*");
    }

    [Fact]
    public void EnsureIsPublishable_WithBlankDescription_ThrowsAndListsDescription()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, "   ", null, null, null, null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Description*");
    }

    [Fact]
    public void EnsureIsPublishable_WithBlankLocation_ThrowsAndListsLocation()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, null, null, "   ", null, null, null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Location*");
    }

    [Fact]
    public void EnsureIsPublishable_WithNoRequiredSkills_ThrowsAndListsRequiredSkills()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, null, null, null, null, new List<string>(), null, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*RequiredSkills*");
    }

    [Fact]
    public void EnsureIsPublishable_WithZeroVacancies_ThrowsAndListsVacancies()
    {
        var job = CreateValidDraft();
        job.UpdateDetails(null, null, null, null, null, null, null, null, 0, null);

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Vacancies*");
    }

    [Fact]
    public void EnsureIsPublishable_WithExpireAtInThePast_ThrowsAndListsExpireAt()
    {
        var job = CreateValidDraft(expireAt: DateTime.UtcNow.AddDays(30));
        // Force an expired date directly through UpdateDetails, bypassing the
        // command validator which would normally reject a past date up front.
        job.UpdateDetails(null, null, null, null, null, null, null, null, null, DateTime.UtcNow.AddDays(-1));

        var act = () => job.EnsureIsPublishable();

        act.Should().Throw<InvalidOperationException>().WithMessage("*ExpireAt*");
    }

    [Fact]
    public void EnsureIsPublishable_WithMultipleMissingFields_ListsAllOfThem()
    {
        var job = CreateValidDraft();
        job.UpdateDetails("   ", "   ", null, null, null, null, null, null, 0, null);

        var act = () => job.EnsureIsPublishable();

        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("Title");
        exception.Message.Should().Contain("CompanyName");
        exception.Message.Should().Contain("Vacancies");
    }
}

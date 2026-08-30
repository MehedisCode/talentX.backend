using FluentAssertions;
using TalentX.Application.Features.Jobs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs;

public class JobRulesTests
{
    private static Job ValidDraft(
        string title = "Backend Engineer",
        string companyName = "Acme Corp",
        string description = "Build and maintain backend services.",
        string location = "Dhaka",
        List<string>? requiredSkills = null,
        int vacancies = 2,
        DateTime? expireAt = null,
        WorkArrangement workArrangement = WorkArrangement.AnywhereFullRemote
    )
    {
        var job = Job.Create(
            "employer-1",
            title,
            companyName,
            description,
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            location,
            workArrangement,
            requiredSkills ?? new List<string> { "C#" },
            vacancies,
            expireAt ?? DateTime.UtcNow.AddDays(30)
        );

        // Set wizard fields based on work arrangement
        if (workArrangement is WorkArrangement.AnywhereFullRemote)
        {
            job.UpdateWizardDetails(
                null,
                null,
                4,
                null
            );
        }
        else if (workArrangement is WorkArrangement.OnSite or WorkArrangement.Hybrid)
        {
            job.UpdateWizardDetails(
                "Dhaka",
                "Bangladesh",
                null,
                null
            );
        }

        return job;
    }

    [Fact]
    public void NormalizeText_TrimsWhitespace()
    {
        JobRules.NormalizeText("  Backend Engineer  ").Should().Be("Backend Engineer");
    }

    [Fact]
    public void NormalizeSkills_TrimsAndDeduplicates_CaseInsensitively()
    {
        var result = JobRules.NormalizeSkills(new List<string> { " C# ", "c#", "SQL", "  " });

        result.Should().BeEquivalentTo(new[] { "C#", "SQL" });
    }

    [Fact]
    public void NormalizeSkills_WithNull_ReturnsEmptyList()
    {
        JobRules.NormalizeSkills(null).Should().BeEmpty();
    }

    [Fact]
    public void EnsurePublishable_OnAFullyValidDraft_DoesNotThrow()
    {
        var job = ValidDraft();

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePublishable_WhenNotDraft_ThrowsInvalidOperationException()
    {
        var job = ValidDraft();
        job.MarkAsPublished();

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft jobs can be published.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsurePublishable_WithBlankTitle_ThrowsAndListsTitle(string title)
    {
        var job = ValidDraft(title: title);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Title*");
    }

    [Fact]
    public void EnsurePublishable_WithBlankCompanyName_ThrowsAndListsCompanyName()
    {
        var job = ValidDraft(companyName: "   ");

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*CompanyName*");
    }

    [Fact]
    public void EnsurePublishable_WithBlankDescription_ThrowsAndListsDescription()
    {
        var job = ValidDraft(description: "   ");

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Description*");
    }

    [Fact]
    public void EnsurePublishable_WithBlankLocation_ThrowsAndListsLocation()
    {
        var job = ValidDraft(location: "   ");

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Location*");
    }

    [Fact]
    public void EnsurePublishable_WithUndefinedEmploymentType_ThrowsAndListsEmploymentType()
    {
        var job = ValidDraft();
        job.SetDetails(
            job.Title, job.CompanyName, job.Description, (EmploymentType)999,
            job.ExperienceLevel, job.Location, job.WorkArrangement, job.RequiredSkills,
            job.Vacancies, job.ExpireAt
        );

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*EmploymentType*");
    }

    [Fact]
    public void EnsurePublishable_WithUndefinedExperienceLevel_ThrowsAndListsExperienceLevel()
    {
        var job = ValidDraft();
        job.SetDetails(
            job.Title, job.CompanyName, job.Description, job.EmploymentType,
            (ExperienceLevel)999, job.Location, job.WorkArrangement, job.RequiredSkills,
            job.Vacancies, job.ExpireAt
        );

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ExperienceLevel*");
    }

    [Fact]
    public void EnsurePublishable_WithUndefinedWorkArrangement_ThrowsAndListsWorkArrangement()
    {
        var job = ValidDraft();
        job.SetDetails(
            job.Title, job.CompanyName, job.Description, job.EmploymentType,
            job.ExperienceLevel, job.Location, (WorkArrangement)999, job.RequiredSkills,
            job.Vacancies, job.ExpireAt
        );

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*WorkArrangement*");
    }

    [Fact]
    public void EnsurePublishable_WithNoRequiredSkills_ThrowsAndListsRequiredSkills()
    {
        var job = ValidDraft(requiredSkills: new List<string>());

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RequiredSkills*");
    }

    [Fact]
    public void EnsurePublishable_WithZeroVacancies_ThrowsAndListsVacancies()
    {
        var job = ValidDraft(vacancies: 0);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Vacancies*");
    }

    [Fact]
    public void EnsurePublishable_WithExpireAtInThePast_ThrowsAndListsExpireAt()
    {
        var job = ValidDraft(expireAt: DateTime.UtcNow.AddDays(-1));

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ExpireAt*");
    }

    [Fact]
    public void EnsurePublishable_WithMultipleMissingFields_ListsAllOfThem()
    {
        var job = ValidDraft(title: "   ", companyName: "   ", vacancies: 0);

        var act = () => JobRules.EnsurePublishable(job);

        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("Title");
        exception.Message.Should().Contain("CompanyName");
        exception.Message.Should().Contain("Vacancies");
    }

    [Fact]
    public void EnsurePublishable_OnSiteWithCityAndCountry_DoesNotThrow()
    {
        var job = ValidDraft(workArrangement: WorkArrangement.OnSite);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePublishable_OnSiteWithoutCity_ThrowsAndListsCity()
    {
        var job = ValidDraft(workArrangement: WorkArrangement.OnSite);
        job.UpdateWizardDetails(null, "Bangladesh", null, null);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*City*");
    }

    [Fact]
    public void EnsurePublishable_OnSiteWithoutCountry_ThrowsAndListsCountry()
    {
        var job = ValidDraft(workArrangement: WorkArrangement.OnSite);
        job.UpdateWizardDetails("Dhaka", null, null, null);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Country*");
    }

    [Fact]
    public void EnsurePublishable_AnywhereFullRemoteWithTimezoneOverlap_DoesNotThrow()
    {
        var job = ValidDraft(workArrangement: WorkArrangement.AnywhereFullRemote);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePublishable_AnywhereFullRemoteWithoutMinOverlapHours_ThrowsAndListsMinOverlapHours()
    {
        var job = ValidDraft(workArrangement: WorkArrangement.AnywhereFullRemote);
        job.UpdateWizardDetails(null, null, null, null);

        var act = () => JobRules.EnsurePublishable(job);

        act.Should().Throw<InvalidOperationException>().WithMessage("*MinOverlapHours*");
    }
}

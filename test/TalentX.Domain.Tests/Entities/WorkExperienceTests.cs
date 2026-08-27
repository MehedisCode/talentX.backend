using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class WorkExperienceTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var beforeCreation = DateTime.UtcNow;
        var workExperience = new WorkExperience(
            userId: "auth0|user-1",
            companyName: "Acme Corp",
            jobTitle: "Senior Backend Developer",
            startDate: "2020-01",
            endDate: "Present",
            isCurrent: true);
        var afterCreation = DateTime.UtcNow;

        workExperience.UserId.Should().Be("auth0|user-1");
        workExperience.CompanyName.Should().Be("Acme Corp");
        workExperience.JobTitle.Should().Be("Senior Backend Developer");
        workExperience.StartDate.Should().Be("2020-01");
        workExperience.EndDate.Should().Be("Present");
        workExperience.IsCurrent.Should().BeTrue();
        workExperience.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        workExperience.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_UpdatesPropertiesAndTimestamp()
    {
        var workExperience = new WorkExperience(
            userId: "auth0|user-1",
            companyName: "Old Corp",
            jobTitle: "Junior Developer",
            startDate: "2018-01",
            endDate: "2019-12",
            isCurrent: false);

        var beforeUpdate = DateTime.UtcNow;
        workExperience.Update(
            companyName: "New Corp",
            jobTitle: "Lead Developer",
            startDate: "2020-01",
            endDate: null,
            isCurrent: true);
        var afterUpdate = DateTime.UtcNow;

        workExperience.CompanyName.Should().Be("New Corp");
        workExperience.JobTitle.Should().Be("Lead Developer");
        workExperience.StartDate.Should().Be("2020-01");
        workExperience.EndDate.Should().BeNull();
        workExperience.IsCurrent.Should().BeTrue();
        workExperience.UpdatedAt.Should().NotBeNull();
        workExperience.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }
}

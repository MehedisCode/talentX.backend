using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class EducationTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var beforeCreation = DateTime.UtcNow;
        var education = new Education(
            userId: "auth0|user-1",
            institution: "MIT",
            degree: "B.Sc. in Computer Science",
            startDate: "2016-09",
            endDate: "2020-05");
        var afterCreation = DateTime.UtcNow;

        education.UserId.Should().Be("auth0|user-1");
        education.Institution.Should().Be("MIT");
        education.Degree.Should().Be("B.Sc. in Computer Science");
        education.StartDate.Should().Be("2016-09");
        education.EndDate.Should().Be("2020-05");
        education.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        education.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_UpdatesPropertiesAndTimestamp()
    {
        var education = new Education(
            userId: "auth0|user-1",
            institution: "Stanford University",
            degree: "B.Sc.",
            startDate: "2016-09",
            endDate: "2020-05");

        var beforeUpdate = DateTime.UtcNow;
        education.Update(
            institution: "Stanford University",
            degree: "M.Sc. in Artificial Intelligence",
            startDate: "2020-09",
            endDate: "2022-06");
        var afterUpdate = DateTime.UtcNow;

        education.Institution.Should().Be("Stanford University");
        education.Degree.Should().Be("M.Sc. in Artificial Intelligence");
        education.StartDate.Should().Be("2020-09");
        education.EndDate.Should().Be("2022-06");
        education.UpdatedAt.Should().NotBeNull();
        education.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }
}

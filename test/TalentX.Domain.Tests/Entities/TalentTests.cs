using FluentAssertions;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class TalentTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");

        talent.UserId.Should().Be("auth0|123");
        talent.Name.Should().Be("John Doe");
        talent.Email.Should().Be("john@example.com");
        talent.Role.Should().Be(UserRole.Talent);
        talent.IsVerified.Should().BeFalse();
        talent.ProfilePictureStorageKey.Should().BeNull();
        talent.Tagline.Should().BeNull();
        talent.ShortBio.Should().BeNull();
        talent.Location.Should().BeNull();
        talent.Phone.Should().BeNull();
        talent.LinkedInUrl.Should().BeNull();
        talent.AdditionalUrls.Should().BeEmpty();
        talent.VideoIntroStorageKey.Should().BeNull();
        talent.TotalYearsExperience.Should().BeNull();
        talent.ExpectedSalary.Should().BeNull();
        talent.SkillPropositions.Should().BeEmpty();
        talent.TechnicalSkills.Should().BeEmpty();
        talent.SoftSkills.Should().BeEmpty();
        talent.ToolsAndPlatforms.Should().BeEmpty();
        talent.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_UpdatesAllProvidedFieldsAndTimestamp()
    {
        var talent = new Talent("auth0|123", "Old Name", "john@example.com");
        var beforeUpdate = DateTime.UtcNow;
        var additionalUrls = new List<string> { "https://github.com/johndoe", "https://johndoe.dev" };
        var skillPropositions = new List<string> { "Full Stack Development", "Cloud Architecture" };

        talent.UpdateProfile(
            name: "New Name",
            tagline: "Senior Software Engineer",
            shortBio: "Passionate about distributed systems",
            location: "Stockholm, Sweden",
            phone: "+46123456789",
            linkedInUrl: "https://linkedin.com/in/johndoe",
            additionalUrls: additionalUrls,
            totalYearsExperience: 8.5m,
            expectedSalary: 120000m,
            skillPropositions: skillPropositions);

        var afterUpdate = DateTime.UtcNow;

        talent.Name.Should().Be("New Name");
        talent.Tagline.Should().Be("Senior Software Engineer");
        talent.ShortBio.Should().Be("Passionate about distributed systems");
        talent.Location.Should().Be("Stockholm, Sweden");
        talent.Phone.Should().Be("+46123456789");
        talent.LinkedInUrl.Should().Be("https://linkedin.com/in/johndoe");
        talent.AdditionalUrls.Should().BeEquivalentTo(additionalUrls);
        talent.TotalYearsExperience.Should().Be(8.5m);
        talent.ExpectedSalary.Should().Be(120000m);
        talent.SkillPropositions.Should().BeEquivalentTo(skillPropositions);
        talent.UpdatedAt.Should().NotBeNull();
        talent.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateProfile_WithNullCollections_InitializesEmptyLists()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");

        talent.UpdateProfile(
            name: "John Doe",
            tagline: null,
            shortBio: null,
            location: null,
            phone: null,
            linkedInUrl: null,
            additionalUrls: null,
            totalYearsExperience: null,
            expectedSalary: null,
            skillPropositions: null);

        talent.AdditionalUrls.Should().NotBeNull().And.BeEmpty();
        talent.SkillPropositions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UpdateProfilePicture_UpdatesKeyAndTimestamp()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");
        var beforeUpdate = DateTime.UtcNow;

        talent.UpdateProfilePicture("avatars/user-123.jpg");

        var afterUpdate = DateTime.UtcNow;

        talent.ProfilePictureStorageKey.Should().Be("avatars/user-123.jpg");
        talent.UpdatedAt.Should().NotBeNull();
        talent.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateVideoIntro_UpdatesKeyAndTimestamp()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");
        var beforeUpdate = DateTime.UtcNow;

        talent.UpdateVideoIntro("videos/user-123.mp4");

        var afterUpdate = DateTime.UtcNow;

        talent.VideoIntroStorageKey.Should().Be("videos/user-123.mp4");
        talent.UpdatedAt.Should().NotBeNull();
        talent.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateSkills_UpdatesCollectionsAndTimestamp()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");
        var technicalSkills = new List<string> { "C#", "PostgreSQL", "Docker" };
        var softSkills = new List<string> { "Leadership", "Communication" };
        var toolsAndPlatforms = new List<string> { "AWS", "GitHub Actions" };
        var beforeUpdate = DateTime.UtcNow;

        talent.UpdateSkills(technicalSkills, softSkills, toolsAndPlatforms);

        var afterUpdate = DateTime.UtcNow;

        talent.TechnicalSkills.Should().BeEquivalentTo(technicalSkills);
        talent.SoftSkills.Should().BeEquivalentTo(softSkills);
        talent.ToolsAndPlatforms.Should().BeEquivalentTo(toolsAndPlatforms);
        talent.UpdatedAt.Should().NotBeNull();
        talent.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateSkills_WithNullParameters_InitializesEmptyLists()
    {
        var talent = new Talent("auth0|123", "John Doe", "john@example.com");

        talent.UpdateSkills(null, null, null);

        talent.TechnicalSkills.Should().NotBeNull().And.BeEmpty();
        talent.SoftSkills.Should().NotBeNull().And.BeEmpty();
        talent.ToolsAndPlatforms.Should().NotBeNull().And.BeEmpty();
    }
}

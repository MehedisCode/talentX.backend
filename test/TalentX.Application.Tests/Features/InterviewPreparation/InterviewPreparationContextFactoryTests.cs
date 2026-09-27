using FluentAssertions;
using TalentX.Application.Features.InterviewPreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.InterviewPreparation;

public class InterviewPreparationContextFactoryTests
{
    private const string TalentUserId = "talent-1";

    private static Job CreateJob()
    {
        return Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme",
            "Build scalable systems",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Remote",
            WorkArrangement.FullRemote,
            new List<string> { "C#", ".NET" },
            1,
            DateTime.UtcNow.AddDays(30));
    }

    private static Talent CreateTalent()
    {
        var talent = new Talent(TalentUserId, "Ada Lovelace", "ada@example.com");
        talent.UpdateProfile(
            "Ada Lovelace",
            "Distributed systems engineer",
            "I build reliable backends",
            "London",
            null,
            null,
            null,
            6,
            null,
            null);
        talent.UpdateSkills(
            new List<string> { "C#" },
            new List<string> { "Mentoring" },
            new List<string> { "Docker" });
        return talent;
    }

    private static InterviewInvitation CreateInvitation(Guid jobId)
    {
        var invitation = new InterviewInvitation(
            jobId, Guid.NewGuid(), "employer-1", TalentUserId, InterviewType.OnCall);
        invitation.Accept(selectedTimeslotId: null, scheduledAt: DateTime.UtcNow.AddHours(2));
        return invitation;
    }

    [Fact]
    public void CreateContext_MapsJobFields()
    {
        // Arrange
        var job = CreateJob();

        // Act
        var context = InterviewPreparationContextFactory.CreateContext(
            job, CreateTalent(), CreateInvitation(job.Id),
            new List<WorkExperience>(), new List<Education>());

        // Assert
        context.JobTitle.Should().Be("Backend Engineer");
        context.CompanyName.Should().Be("Acme");
        context.JobDescription.Should().Be("Build scalable systems");
        context.RequiredSkills.Should().Equal("C#", ".NET");
        context.ExperienceLevel.Should().Be("MidLevel");
        context.EmploymentType.Should().Be("FullTime");
        context.WorkArrangement.Should().Be("FullRemote");
    }

    [Fact]
    public void CreateContext_MapsTalentProfileFields()
    {
        // Arrange
        var job = CreateJob();

        // Act
        var context = InterviewPreparationContextFactory.CreateContext(
            job, CreateTalent(), CreateInvitation(job.Id),
            new List<WorkExperience>(), new List<Education>());

        // Assert
        context.TalentName.Should().Be("Ada Lovelace");
        context.Tagline.Should().Be("Distributed systems engineer");
        context.ShortBio.Should().Be("I build reliable backends");
        context.TotalYearsExperience.Should().Be(6);
        context.TechnicalSkills.Should().Equal("C#");
        context.SoftSkills.Should().Equal("Mentoring");
        context.ToolsAndPlatforms.Should().Equal("Docker");
    }

    [Fact]
    public void CreateContext_BuildsSnapshotsFromHistory()
    {
        // Arrange
        var job = CreateJob();
        var workExperiences = new List<WorkExperience>
        {
            new(TalentUserId, "Globex", "Backend Engineer", "2020", null, true),
            new(TalentUserId, "Initech", "Junior Developer", null, "2019", false),
        };
        var educations = new List<Education>
        {
            new(TalentUserId, "MIT", "BSc Computer Science", "2014", "2018"),
        };

        // Act
        var context = InterviewPreparationContextFactory.CreateContext(
            job, CreateTalent(), CreateInvitation(job.Id), workExperiences, educations);

        // Assert
        context.WorkExperiences.Should().HaveCount(2);
        context.WorkExperiences[0].Should().Be(
            new WorkExperienceSnapshot("Globex", "Backend Engineer", "2020", null, true));
        context.WorkExperiences[1].StartDate.Should().BeEmpty();
        context.Educations.Should().ContainSingle()
            .Which.Should().Be(new EducationSnapshot("MIT", "BSc Computer Science", "2014", "2018"));
    }

    [Fact]
    public void CreateContext_MapsInvitationFields()
    {
        // Arrange
        var job = CreateJob();
        var invitation = CreateInvitation(job.Id);

        // Act
        var context = InterviewPreparationContextFactory.CreateContext(
            job, CreateTalent(), invitation,
            new List<WorkExperience>(), new List<Education>());

        // Assert
        context.InterviewType.Should().Be("OnCall");
        context.ScheduledAt.Should().Be(invitation.ScheduledAt);
    }

    [Fact]
    public void CreateContext_WithNoHistory_ReturnsEmptySnapshotLists()
    {
        // Arrange
        var job = CreateJob();

        // Act
        var context = InterviewPreparationContextFactory.CreateContext(
            job, CreateTalent(), CreateInvitation(job.Id),
            new List<WorkExperience>(), new List<Education>());

        // Assert
        context.WorkExperiences.Should().BeEmpty();
        context.Educations.Should().BeEmpty();
    }
}

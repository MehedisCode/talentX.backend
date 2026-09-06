using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public sealed class JobApplicationTests
{
    private static JobApplication CreateJobApplication(
        Guid? jobId = null,
        string? talentUserId = "talent-user-123",
        string applicantName = "Jane Doe",
        string applicantEmail = "jane.doe@example.com",
        string resumeStorageKey = "resumes/jane-doe-resume.pdf",
        string? coverLetter = "Excited to apply for this role.",
        decimal? expectedSalary = 8500m,
        decimal? totalYearsExperience = 5.5m,
        string? location = "Berlin, Germany")
    {
        return new JobApplication(
            jobId ?? Guid.NewGuid(),
            talentUserId,
            applicantName,
            applicantEmail,
            resumeStorageKey,
            coverLetter,
            expectedSalary,
            totalYearsExperience,
            location);
    }

    [Fact]
    public void Constructor_SetsAllPropertiesAndInitialStatus()
    {
        var targetJobId = Guid.NewGuid();
        var beforeCreation = DateTime.UtcNow;

        var application = new JobApplication(
            targetJobId,
            "talent-456",
            "Alex Smith",
            "alex@example.com",
            "resumes/alex.pdf",
            "My cover letter",
            10000m,
            6.0m,
            "London, UK");

        var afterCreation = DateTime.UtcNow;

        application.JobId.Should().Be(targetJobId);
        application.TalentUserId.Should().Be("talent-456");
        application.ApplicantName.Should().Be("Alex Smith");
        application.ApplicantEmail.Should().Be("alex@example.com");
        application.ResumeStorageKey.Should().Be("resumes/alex.pdf");
        application.CoverLetter.Should().Be("My cover letter");
        application.ExpectedSalary.Should().Be(10000m);
        application.TotalYearsExperience.Should().Be(6.0m);
        application.Location.Should().Be("London, UK");
        application.Status.Should().Be(ApplicationStatus.Applied);
        application.AppliedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        application.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        application.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void CreateGuestApplication_SetsStatusToPendingVerification()
    {
        var targetJobId = Guid.NewGuid();
        var application = JobApplication.CreateGuestApplication(
            targetJobId,
            "Alex Smith",
            "alex@example.com",
            "resumes/alex.pdf");

        application.JobId.Should().Be(targetJobId);
        application.TalentUserId.Should().BeNull();
        application.ApplicantName.Should().Be("Alex Smith");
        application.ApplicantEmail.Should().Be("alex@example.com");
        application.ResumeStorageKey.Should().Be("resumes/alex.pdf");
        application.Status.Should().Be(ApplicationStatus.PendingVerification);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusAndSetsUpdatedAt()
    {
        var application = CreateJobApplication();
        var beforeUpdate = DateTime.UtcNow;

        application.UpdateStatus(ApplicationStatus.Shortlisted);

        var afterUpdate = DateTime.UtcNow;

        application.Status.Should().Be(ApplicationStatus.Shortlisted);
        application.UpdatedAt.Should().NotBeNull();
        application.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }

    [Fact]
    public void UpdateDetails_UpdatesFieldsAndSetsUpdatedAt()
    {
        var application = CreateJobApplication();
        var beforeUpdate = DateTime.UtcNow;

        application.UpdateDetails(
            "Jane Updated",
            "jane.updated@example.com",
            "resumes/jane-new.pdf",
            "Updated cover letter",
            9500m,
            6.5m,
            "Munich, Germany");

        var afterUpdate = DateTime.UtcNow;

        application.ApplicantName.Should().Be("Jane Updated");
        application.ApplicantEmail.Should().Be("jane.updated@example.com");
        application.ResumeStorageKey.Should().Be("resumes/jane-new.pdf");
        application.CoverLetter.Should().Be("Updated cover letter");
        application.ExpectedSalary.Should().Be(9500m);
        application.TotalYearsExperience.Should().Be(6.5m);
        application.Location.Should().Be("Munich, Germany");
        application.UpdatedAt.Should().NotBeNull();
        application.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }
}

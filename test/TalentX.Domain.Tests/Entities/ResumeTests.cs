using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class ResumeTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var beforeCreation = DateTime.UtcNow;
        var resume = new Resume(
            userId: "auth0|user-1",
            storageKey: "resumes/user-1/resume.pdf",
            originalFileName: "my_resume.pdf",
            contentType: "application/pdf",
            sizeBytes: 1048576);
        var afterCreation = DateTime.UtcNow;

        resume.UserId.Should().Be("auth0|user-1");
        resume.StorageKey.Should().Be("resumes/user-1/resume.pdf");
        resume.OriginalFileName.Should().Be("my_resume.pdf");
        resume.ContentType.Should().Be("application/pdf");
        resume.SizeBytes.Should().Be(1048576);
        resume.UploadedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        resume.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        resume.UpdatedAt.Should().BeNull();
        resume.DisplayName.Should().Be("my_resume");
        resume.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithExplicitDisplayNameAndDefaultFlag_UsesThem()
    {
        var resume = new Resume(
            userId: "auth0|user-1",
            storageKey: "resumes/user-1/resume.pdf",
            originalFileName: "my_resume.pdf",
            contentType: "application/pdf",
            sizeBytes: 1024,
            displayName: "  Backend Resume  ",
            isDefault: true);

        resume.DisplayName.Should().Be("Backend Resume");
        resume.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Rename_TrimsAndUpdatesTimestamp()
    {
        var resume = new Resume("user-1", "key.pdf", "my_resume.pdf", "application/pdf", 1024);

        var beforeRename = DateTime.UtcNow;
        resume.Rename("  Senior Resume  ");
        var afterRename = DateTime.UtcNow;

        resume.DisplayName.Should().Be("Senior Resume");
        resume.UpdatedAt.Should().NotBeNull();
        resume.UpdatedAt!.Value.Should().BeOnOrAfter(beforeRename).And.BeOnOrBefore(afterRename);
    }

    [Fact]
    public void Rename_WithBlankName_FallsBackToFileName()
    {
        var resume = new Resume("user-1", "key.pdf", "my_resume.pdf", "application/pdf", 1024);

        resume.Rename("   ");

        resume.DisplayName.Should().Be("my_resume");
    }

    [Fact]
    public void SetDefault_And_ClearDefault_ToggleFlag()
    {
        var resume = new Resume("user-1", "key.pdf", "my_resume.pdf", "application/pdf", 1024);

        resume.SetDefault();
        resume.IsDefault.Should().BeTrue();

        resume.ClearDefault();
        resume.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void UpdateFile_UpdatesPropertiesAndTimestamps()
    {
        var resume = new Resume(
            userId: "auth0|user-1",
            storageKey: "resumes/user-1/old.pdf",
            originalFileName: "old.pdf",
            contentType: "application/pdf",
            sizeBytes: 500000);

        var beforeUpdate = DateTime.UtcNow;
        resume.UpdateFile(
            storageKey: "resumes/user-1/new.pdf",
            originalFileName: "new.pdf",
            contentType: "application/pdf",
            sizeBytes: 750000);
        var afterUpdate = DateTime.UtcNow;

        resume.StorageKey.Should().Be("resumes/user-1/new.pdf");
        resume.OriginalFileName.Should().Be("new.pdf");
        resume.ContentType.Should().Be("application/pdf");
        resume.SizeBytes.Should().Be(750000);
        resume.UploadedAt.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
        resume.UpdatedAt.Should().NotBeNull();
        resume.UpdatedAt!.Value.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }
}

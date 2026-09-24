using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class JobReportTests
{
    [Fact]
    public void Constructor_ValidState_SetsProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var reporterId = "user123";
        var reason = JobReportReason.SuspectedScam;
        var details = "Looks like spam";

        // Act
        var report = new JobReport(jobId, reporterId, reason, details);

        // Assert
        report.JobId.Should().Be(jobId);
        report.ReporterUserId.Should().Be(reporterId);
        report.Reason.Should().Be(reason);
        report.AdditionalDetails.Should().Be(details);
        report.Status.Should().Be(JobReportStatus.Pending);
        report.ResolvedAt.Should().BeNull();
        report.ResolvedByUserId.Should().BeNull();
    }

    [Fact]
    public void Resolve_PendingStatus_SetsResolvedStatus()
    {
        // Arrange
        var report = new JobReport(Guid.NewGuid(), "user1", JobReportReason.SuspectedScam, null);
        var adminId = "admin123";
        var notes = "Resolved this";

        // Act
        report.Resolve(adminId, notes);

        // Assert
        report.Status.Should().Be(JobReportStatus.Resolved);
        report.ResolvedAt.Should().NotBeNull();
        report.ResolvedByUserId.Should().Be(adminId);
        report.AdminNotes.Should().Be(notes);
    }

    [Fact]
    public void Resolve_NotPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var report = new JobReport(Guid.NewGuid(), "user1", JobReportReason.SuspectedScam, null);
        report.Resolve("admin123", null);

        // Act
        var act = () => report.Resolve("admin456", null);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Dismiss_PendingStatus_SetsDismissedStatus()
    {
        // Arrange
        var report = new JobReport(Guid.NewGuid(), "user1", JobReportReason.SuspectedScam, null);
        var adminId = "admin123";
        var notes = "Dismissed this";

        // Act
        report.Dismiss(adminId, notes);

        // Assert
        report.Status.Should().Be(JobReportStatus.Dismissed);
        report.ResolvedAt.Should().NotBeNull();
        report.ResolvedByUserId.Should().Be(adminId);
        report.AdminNotes.Should().Be(notes);
    }

    [Fact]
    public void Dismiss_NotPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var report = new JobReport(Guid.NewGuid(), "user1", JobReportReason.SuspectedScam, null);
        report.Dismiss("admin123", null);

        // Act
        var act = () => report.Dismiss("admin456", null);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}

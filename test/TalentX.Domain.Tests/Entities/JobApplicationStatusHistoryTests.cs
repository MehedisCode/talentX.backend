using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public sealed class JobApplicationStatusHistoryTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var jobApplicationId = Guid.NewGuid();
        var beforeCreation = DateTime.UtcNow;

        var history = JobApplicationStatusHistory.Create(
            jobApplicationId,
            ApplicationStatus.Applied,
            ApplicationStatus.Shortlisted,
            "employer-1");

        var afterCreation = DateTime.UtcNow;

        history.JobApplicationId.Should().Be(jobApplicationId);
        history.FromStatus.Should().Be(ApplicationStatus.Applied);
        history.ToStatus.Should().Be(ApplicationStatus.Shortlisted);
        history.ChangedByUserId.Should().Be("employer-1");
        history.ChangedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Create_AllowsNullFromStatusForInitialEntry()
    {
        var history = JobApplicationStatusHistory.Create(
            Guid.NewGuid(),
            null,
            ApplicationStatus.PendingVerification,
            "alex@example.com");

        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(ApplicationStatus.PendingVerification);
    }
}

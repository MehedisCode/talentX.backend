using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class InvitationTests
{
    [Fact]
    public void Constructor_InitializesWithActiveStatusAndTimestamps()
    {
        var jobId = Guid.NewGuid();
        var talentUserId = "talent-usr-123";
        var before = DateTime.UtcNow;

        var invitation = new Invitation(jobId, talentUserId);
        var after = DateTime.UtcNow;

        invitation.Id.Should().NotBeEmpty();
        invitation.JobId.Should().Be(jobId);
        invitation.TalentUserId.Should().Be(talentUserId);
        invitation.IsActive.Should().BeTrue();
        invitation.InvitedDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        invitation.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        invitation.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalseAndUpdatesTimestamp()
    {
        var jobId = Guid.NewGuid();
        var invitation = new Invitation(jobId, "talent-456");

        var before = DateTime.UtcNow;
        invitation.Deactivate();
        var after = DateTime.UtcNow;

        invitation.IsActive.Should().BeFalse();
        invitation.UpdatedAt.Should().NotBeNull();
        invitation.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}

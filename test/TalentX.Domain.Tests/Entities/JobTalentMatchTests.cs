using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class JobTalentMatchTests
{
    [Fact]
    public void Constructor_SetsAllPropertiesCorrectly()
    {
        var jobId = Guid.NewGuid();
        var talentUserId = "talent-999";
        var score = 88.5m;
        var explanation = "Great match for full stack developer.";
        var before = DateTime.UtcNow;

        var match = new JobTalentMatch(jobId, talentUserId, score, explanation);
        var after = DateTime.UtcNow;

        match.Id.Should().NotBeEmpty();
        match.JobId.Should().Be(jobId);
        match.TalentUserId.Should().Be(talentUserId);
        match.Score.Should().Be(score);
        match.CriteriaSnapshot.Should().Be(explanation);
        match.MatchedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        match.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        match.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateScore_UpdatesScoreAndTimestamp()
    {
        var jobId = Guid.NewGuid();
        var match = new JobTalentMatch(jobId, "talent-999", 70.0m, "Initial");

        var before = DateTime.UtcNow;
        match.UpdateScore(92.0m, "Updated explanation");
        var after = DateTime.UtcNow;

        match.Score.Should().Be(92.0m);
        match.CriteriaSnapshot.Should().Be("Updated explanation");
        match.MatchedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        match.UpdatedAt.Should().NotBeNull();
        match.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}

using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class WorkExperienceNormalizerTests
{
    [Theory]
    [InlineData(true, null, true)]
    [InlineData(false, "Present", true)]
    [InlineData(null, "Present", true)]
    [InlineData(null, "present", true)]
    [InlineData(null, "Current", true)]
    [InlineData(null, "Ongoing", true)]
    [InlineData(null, "Now", true)]
    [InlineData(false, "Dec 2023", false)]
    [InlineData(null, "2019", false)]
    [InlineData(null, null, false)]
    [InlineData(null, "   ", false)]
    public void IsCurrent_DetectsCurrentRoles(bool? flag, string? endDate, bool expected)
    {
        WorkExperienceNormalizer.IsCurrent(flag, endDate).Should().Be(expected);
    }

    [Theory]
    [InlineData("Current", true, "Present")]
    [InlineData("Present", true, "Present")]
    [InlineData("Dec 2023", false, "Dec 2023")]
    [InlineData("  Dec 2023  ", false, "Dec 2023")]
    [InlineData(null, false, null)]
    [InlineData("   ", false, null)]
    public void NormalizeEndDate_NormalizesCurrentRoles(string? endDate, bool isCurrent, string? expected)
    {
        WorkExperienceNormalizer.NormalizeEndDate(endDate, isCurrent).Should().Be(expected);
    }
}

using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class WorkExperienceNormalizerTests
{
    [Theory]
    [InlineData(true, null, null, false)]
    [InlineData(true, " ", null, false)]
    [InlineData(true, null, "", false)]
    [InlineData(true, null, "Jan 2021", true)]
    [InlineData(true, "Dec 2023", "Jan 2021", true)]
    [InlineData(false, null, null, false)]
    [InlineData(null, null, null, false)]
    [InlineData(null, "Present", null, true)]
    [InlineData(null, null, "Jan 2021", false)]
    public void IsCurrent_DatelessRoleIsNeverCurrent_EvenWhenFlagged(
        bool? flag, string? endDate, string? startDate, bool expected)
    {
        WorkExperienceNormalizer.IsCurrent(flag, endDate, startDate).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "Old job", "Jan 2021", true)]
    [InlineData(true, "Old job", null, true)]
    [InlineData(false, "Present", null, true)]
    [InlineData(null, "Present", null, true)]
    [InlineData(null, "present", null, true)]
    [InlineData(null, "Current", null, true)]
    [InlineData(null, "Ongoing", null, true)]
    [InlineData(null, "Now", null, true)]
    [InlineData(false, "Dec 2023", "Jan 2021", false)]
    [InlineData(null, "2019", "Jan 2021", false)]
    [InlineData(null, null, "Jan 2021", false)]
    [InlineData(null, "   ", "Jan 2021", false)]
    public void IsCurrent_PhoneticEndDates_DetectsCurrentRoles(
        bool? flag, string? endDate, string? startDate, bool expected)
    {
        WorkExperienceNormalizer.IsCurrent(flag, endDate, startDate).Should().Be(expected);
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

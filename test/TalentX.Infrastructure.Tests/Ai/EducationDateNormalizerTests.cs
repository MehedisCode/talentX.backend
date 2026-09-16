using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class EducationDateNormalizerTests
{
    [Theory]
    [InlineData("2019", null, null, "2019")]
    [InlineData("2019.", null, null, "2019.")]
    [InlineData("2019 ", null, null, "2019")]
    [InlineData("Expected 2024", null, null, "Expected 2024")]
    [InlineData("Graduated 2020", null, null, "Graduated 2020")]
    [InlineData("Class of 2018", null, null, "Class of 2018")]
    public void Normalize_MovesLoneCompletionYear_ToEndDate(
        string? startDate,
        string? endDate,
        string? expectedStart,
        string? expectedEnd)
    {
        var (start, end) = EducationDateNormalizer.Normalize(startDate, endDate);

        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }

    [Theory]
    [InlineData("2017", "2021", "2017", "2021")]
    [InlineData("Sep 2016", "2020", "Sep 2016", "2020")]
    [InlineData(null, "2020", null, "2020")]
    [InlineData("Sep 2016", null, "Sep 2016", null)]
    [InlineData(null, null, null, null)]
    [InlineData("  ", "  ", null, null)]
    public void Normalize_KeepsRangesAndNonCompletionValues(
        string? startDate,
        string? endDate,
        string? expectedStart,
        string? expectedEnd)
    {
        var (start, end) = EducationDateNormalizer.Normalize(startDate, endDate);

        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }

    [Theory]
    [InlineData("2016 - 2020", null, "2016", "2020")]
    [InlineData("2016-2020", null, "2016", "2020")]
    [InlineData("2016 to 2020", null, "2016", "2020")]
    [InlineData("Sep 2016 - Jun 2020", null, "Sep 2016", "Jun 2020")]
    [InlineData(null, "2016 - 2020", "2016", "2020")]
    [InlineData("2016-05", null, "2016-05", null)]
    public void Normalize_SplitsRangeReturnedInASingleField(
        string? startDate,
        string? endDate,
        string? expectedStart,
        string? expectedEnd)
    {
        var (start, end) = EducationDateNormalizer.Normalize(startDate, endDate);

        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }

    [Fact]
    public void Normalize_CollapsesDuplicateLoneYear_ToEndDate()
    {
        var (start, end) = EducationDateNormalizer.Normalize("2019", "2019");

        start.Should().BeNull();
        end.Should().Be("2019");
    }
}

using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class ResumeChronologyTests
{
    [Theory]
    [InlineData("Present", ResumeChronology.CurrentKey)]
    [InlineData("current", ResumeChronology.CurrentKey)]
    [InlineData("Ongoing", ResumeChronology.CurrentKey)]
    [InlineData("Jan 2020", 2020 * 12)]
    [InlineData("December 2019", 2019 * 12 + 11)]
    [InlineData("2020-05", 2020 * 12 + 4)]
    [InlineData("05/2020", 2020 * 12 + 4)]
    [InlineData("2020", 2020 * 12)]
    [InlineData("Expected 2024", 2024 * 12)]
    public void ToSortKey_ParsesDates(string value, int expected)
    {
        ResumeChronology.ToSortKey(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("soon")]
    public void ToSortKey_ReturnsNull_ForUnparseableValues(string? value)
    {
        ResumeChronology.ToSortKey(value).Should().BeNull();
    }

    [Fact]
    public void SortNewestFirst_OrdersDescending_WithMissingKeysLast_AndIsStable()
    {
        var items = new[]
        {
            new Item("old", "2017"),
            new Item("unknown-a", null),
            new Item("newest", "Jan 2022"),
            new Item("middle", "2019"),
            new Item("unknown-b", "")
        };

        var sorted = ResumeChronology.SortNewestFirst(items, item => ResumeChronology.ToSortKey(item.Date));

        sorted.Select(item => item.Name).Should().Equal("newest", "middle", "old", "unknown-a", "unknown-b");
    }

    [Fact]
    public void SortNewestFirst_PutsCurrentDatesFirst()
    {
        var items = new[]
        {
            new Item("past", "2017"),
            new Item("current", "Present"),
            new Item("recent", "2023")
        };

        var sorted = ResumeChronology.SortNewestFirst(items, item => ResumeChronology.ToSortKey(item.Date));

        sorted.Select(item => item.Name).Should().Equal("current", "recent", "past");
    }

    private sealed record Item(string Name, string? Date);
}

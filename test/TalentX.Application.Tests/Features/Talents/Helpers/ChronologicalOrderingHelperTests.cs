using FluentAssertions;
using TalentX.Application.Features.Talents.Helpers;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Helpers;

public sealed class ChronologicalOrderingHelperTests
{
    private sealed record WorkExperienceSample(
        string Title,
        string? StartDate,
        string? EndDate,
        bool IsCurrent);

    private sealed record EducationSample(
        string Degree,
        string? StartDate,
        string? EndDate);

    [Theory]
    [InlineData("Present", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("Current", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("currently", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("Ongoing", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("now", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("till date", ChronologicalOrderingHelper.CurrentKey)]
    [InlineData("to date", ChronologicalOrderingHelper.CurrentKey)]
    public void ToSortKey_WhenCurrentIndicator_ReturnsCurrentKey(string input, int expected)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("2025-09", 2025 * 12 + 8)]
    [InlineData("2020-05", 2020 * 12 + 4)]
    [InlineData("2021-01-01", 2021 * 12 + 0)]
    public void ToSortKey_WhenIsoFormat_ReturnsCorrectYearMonthKey(string input, int expected)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("09/2025", 2025 * 12 + 8)]
    [InlineData("04/2022", 2022 * 12 + 3)]
    [InlineData("01/2020", 2020 * 12 + 0)]
    [InlineData("02/2021", 2021 * 12 + 1)]
    public void ToSortKey_WhenSlashFormat_ReturnsCorrectYearMonthKey(string input, int expected)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Sep 2025", 2025 * 12 + 8)]
    [InlineData("September 2025", 2025 * 12 + 8)]
    [InlineData("Jan 2020", 2020 * 12 + 0)]
    [InlineData("December 2019", 2019 * 12 + 11)]
    public void ToSortKey_WhenMonthNameFormat_ReturnsCorrectYearMonthKey(string input, int expected)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("2025", 2025 * 12)]
    [InlineData("Expected 2024", 2024 * 12)]
    public void ToSortKey_WhenYearOnlyOrContainsYear_ReturnsYearKey(string input, int expected)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("soon")]
    public void ToSortKey_WhenUnparseable_ReturnsNull(string? input)
    {
        ChronologicalOrderingHelper.ToSortKey(input).Should().BeNull();
    }

    [Fact]
    public void SortWorkExperiences_BugScenarioTX26_440_OrdersReverseChronologically()
    {
        var rawExperiences = new List<WorkExperienceSample>
        {
            new("Senior Software Test Engineer", "04/2022", "09/2025", false),
            new("SQA Engineer", "04/2019", "01/2020", false),
            new("Quality Excellence Lead", "09/2025", "Present", true),
            new("SQA Engineer", "02/2021", "02/2022", false)
        };

        var sortedExperiences = ChronologicalOrderingHelper.SortWorkExperiences(
            rawExperiences,
            experience => (experience.IsCurrent, experience.StartDate, experience.EndDate));

        sortedExperiences.Select(experience => experience.Title).Should().Equal(
            "Quality Excellence Lead",
            "Senior Software Test Engineer",
            "SQA Engineer",
            "SQA Engineer");

        sortedExperiences.Select(experience => (experience.Title, experience.StartDate, experience.EndDate)).Should().Equal(
            ("Quality Excellence Lead", "09/2025", "Present"),
            ("Senior Software Test Engineer", "04/2022", "09/2025"),
            ("SQA Engineer", "02/2021", "02/2022"),
            ("SQA Engineer", "04/2019", "01/2020"));
    }

    [Fact]
    public void SortWorkExperiences_WhenMultipleCurrentRolesExist_TieBreaksByStartDateDescending()
    {
        var experiences = new List<WorkExperienceSample>
        {
            new("Older Current Role", "01/2023", "Present", true),
            new("Newer Current Role", "05/2024", "Present", true),
            new("Past Role", "01/2020", "12/2022", false)
        };

        var sorted = ChronologicalOrderingHelper.SortWorkExperiences(
            experiences,
            experience => (experience.IsCurrent, experience.StartDate, experience.EndDate));

        sorted.Select(experience => experience.Title).Should().Equal(
            "Newer Current Role",
            "Older Current Role",
            "Past Role");
    }

    [Fact]
    public void SortWorkExperiences_WhenMissingOrNullDates_SinksToBottomStably()
    {
        var experiences = new List<WorkExperienceSample>
        {
            new("Missing Dates A", null, null, false),
            new("Valid Role Recent", "01/2023", "12/2023", false),
            new("Missing Dates B", "unparseable", "", false),
            new("Valid Role Past", "01/2020", "12/2020", false)
        };

        var sorted = ChronologicalOrderingHelper.SortWorkExperiences(
            experiences,
            experience => (experience.IsCurrent, experience.StartDate, experience.EndDate));

        sorted.Select(experience => experience.Title).Should().Equal(
            "Valid Role Recent",
            "Valid Role Past",
            "Missing Dates A",
            "Missing Dates B");
    }

    [Fact]
    public void SortWorkExperiences_WhenEndDateMissing_UsesStartDateForSort()
    {
        var experiences = new List<WorkExperienceSample>
        {
            new("Older Role With Start Only", "01/2021", null, false),
            new("Newer Role With Start Only", "01/2023", null, false),
            new("Completed Role", "01/2020", "01/2022", false)
        };

        var sorted = ChronologicalOrderingHelper.SortWorkExperiences(
            experiences,
            experience => (experience.IsCurrent, experience.StartDate, experience.EndDate));

        sorted.Select(experience => experience.Title).Should().Equal(
            "Newer Role With Start Only",
            "Completed Role",
            "Older Role With Start Only");
    }

    [Fact]
    public void SortEducations_OrdersDescendingByEndDateAndStartDate()
    {
        var educations = new List<EducationSample>
        {
            new("B.Sc. Computer Science", "2016", "2020"),
            new("Ongoing Ph.D.", "2023", "Present"),
            new("M.Sc. Software Engineering", "2020", "2022"),
            new("High School", "2012", "2016")
        };

        var sorted = ChronologicalOrderingHelper.SortEducations(
            educations,
            education => (education.StartDate, education.EndDate));

        sorted.Select(education => education.Degree).Should().Equal(
            "Ongoing Ph.D.",
            "M.Sc. Software Engineering",
            "B.Sc. Computer Science",
            "High School");
    }

    [Fact]
    public void SortEducations_WhenEndDatesAreEqual_TieBreaksByStartDateDescending()
    {
        var educations = new List<EducationSample>
        {
            new("Long Degree", "2015", "2020"),
            new("Short Degree", "2018", "2020")
        };

        var sorted = ChronologicalOrderingHelper.SortEducations(
            educations,
            education => (education.StartDate, education.EndDate));

        sorted.Select(education => education.Degree).Should().Equal(
            "Short Degree",
            "Long Degree");
    }

    [Fact]
    public void SortEducations_WhenMissingDates_SinksToBottomStably()
    {
        var educations = new List<EducationSample>
        {
            new("Missing A", null, null),
            new("Recent Education", "2020", "2024"),
            new("Missing B", "", "not a date")
        };

        var sorted = ChronologicalOrderingHelper.SortEducations(
            educations,
            education => (education.StartDate, education.EndDate));

        sorted.Select(education => education.Degree).Should().Equal(
            "Recent Education",
            "Missing A",
            "Missing B");
    }
}

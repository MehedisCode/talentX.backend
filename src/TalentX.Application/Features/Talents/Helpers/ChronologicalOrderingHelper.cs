using System.Text.RegularExpressions;

namespace TalentX.Application.Features.Talents.Helpers;

public static class ChronologicalOrderingHelper
{
    public const int CurrentKey = int.MaxValue;

    private static readonly string[] MonthPrefixes =
    [
        "jan", "feb", "mar", "apr", "may", "jun",
        "jul", "aug", "sep", "oct", "nov", "dec"
    ];

    private static readonly Regex CurrentRegex = new(
        @"\b(present|current|currently|now|ongoing|till date|to date)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MonthYearRegex = new(
        @"^(?<month>[A-Za-z]{3,9})\.?,?\s+(?<year>(?:19|20)\d{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IsoRegex = new(
        @"^(?<year>(?:19|20)\d{2})-(?<month>\d{1,2})(?:-\d{1,2})?$",
        RegexOptions.Compiled);

    private static readonly Regex SlashRegex = new(
        @"^(?<month>\d{1,2})\/(?:(?:\d{1,2})\/)?(?<year>(?:19|20)\d{2})$",
        RegexOptions.Compiled);

    private static readonly Regex YearOnlyRegex = new(
        @"^(?<year>(?:19|20)\d{2})$",
        RegexOptions.Compiled);

    private static readonly Regex AnyYearRegex = new(
        @"\b(?<year>(?:19|20)\d{2})\b",
        RegexOptions.Compiled);

    public static int? ToSortKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedText = value.Trim();

        if (CurrentRegex.IsMatch(trimmedText))
        {
            return CurrentKey;
        }

        var monthYearMatch = MonthYearRegex.Match(trimmedText);
        if (monthYearMatch.Success)
        {
            var monthIndex = MonthIndex(monthYearMatch.Groups["month"].Value);
            var year = int.Parse(monthYearMatch.Groups["year"].Value);
            return year * 12 + (monthIndex ?? 0);
        }

        var isoMatch = IsoRegex.Match(trimmedText);
        if (isoMatch.Success && TryReadMonth(isoMatch.Groups["month"].Value, out var isoMonth))
        {
            return int.Parse(isoMatch.Groups["year"].Value) * 12 + isoMonth;
        }

        var slashMatch = SlashRegex.Match(trimmedText);
        if (slashMatch.Success && TryReadMonth(slashMatch.Groups["month"].Value, out var slashMonth))
        {
            return int.Parse(slashMatch.Groups["year"].Value) * 12 + slashMonth;
        }

        var yearOnlyMatch = YearOnlyRegex.Match(trimmedText);
        if (yearOnlyMatch.Success)
        {
            return int.Parse(yearOnlyMatch.Groups["year"].Value) * 12;
        }

        var anyYearMatch = AnyYearRegex.Match(trimmedText);
        if (anyYearMatch.Success)
        {
            return int.Parse(anyYearMatch.Groups["year"].Value) * 12;
        }

        return null;
    }

    public static List<T> SortWorkExperiences<T>(
        IEnumerable<T> items,
        Func<T, (bool IsCurrent, string? StartDate, string? EndDate)> selector)
    {
        return items
            .Select((item, index) =>
            {
                var (isCurrent, startDate, endDate) = selector(item);
                var startDateKey = ToSortKey(startDate);
                var endDateKey = ToSortKey(endDate);

                var primarySortKey = isCurrent ? CurrentKey : endDateKey ?? startDateKey;
                var secondarySortKey = startDateKey;

                return (Item: item, Index: index, PrimaryKey: primarySortKey, SecondaryKey: secondarySortKey);
            })
            .OrderBy(entry => entry.PrimaryKey.HasValue ? 0 : 1)
            .ThenByDescending(entry => entry.PrimaryKey ?? int.MinValue)
            .ThenByDescending(entry => entry.SecondaryKey ?? int.MinValue)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item)
            .ToList();
    }

    public static List<T> SortEducations<T>(
        IEnumerable<T> items,
        Func<T, (string? StartDate, string? EndDate)> selector)
    {
        return items
            .Select((item, index) =>
            {
                var (startDate, endDate) = selector(item);
                var startDateKey = ToSortKey(startDate);
                var endDateKey = ToSortKey(endDate);

                var primarySortKey = endDateKey ?? startDateKey;
                var secondarySortKey = startDateKey;

                return (Item: item, Index: index, PrimaryKey: primarySortKey, SecondaryKey: secondarySortKey);
            })
            .OrderBy(entry => entry.PrimaryKey.HasValue ? 0 : 1)
            .ThenByDescending(entry => entry.PrimaryKey ?? int.MinValue)
            .ThenByDescending(entry => entry.SecondaryKey ?? int.MinValue)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item)
            .ToList();
    }

    public static List<T> SortNewestFirst<T>(
        IEnumerable<T> items,
        Func<T, int?> selectKey)
    {
        return items
            .Select((item, index) => (Item: item, Index: index, Key: selectKey(item)))
            .OrderBy(entry => entry.Key.HasValue ? 0 : 1)
            .ThenByDescending(entry => entry.Key ?? int.MinValue)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Item)
            .ToList();
    }

    private static bool TryReadMonth(string rawMonth, out int monthIndex)
    {
        monthIndex = int.Parse(rawMonth) - 1;
        return monthIndex is >= 0 and <= 11;
    }

    private static int? MonthIndex(string monthName)
    {
        var trimmedMonth = monthName.Trim().ToLowerInvariant();
        if (trimmedMonth.Length < 3)
        {
            return null;
        }

        var index = Array.IndexOf(MonthPrefixes, trimmedMonth[..3]);
        return index == -1 ? null : index;
    }
}

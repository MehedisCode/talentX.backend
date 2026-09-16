using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Turns the free-form dates found on a resume ("Jan 2020", "2020-01",
/// "01/2020", "2020", "Present", "") into comparable sort keys, and provides a
/// stable newest-first ordering so work-experience and education entries are
/// returned in a predictable order.
/// </summary>
public static class ResumeChronology
{
    /// <summary>Sort key used for current/ongoing dates so they order first.</summary>
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
        @"^(?<month>[A-Za-z]{3,9})\.?\s+(?<year>(?:19|20)\d{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IsoRegex = new(
        @"^(?<year>(?:19|20)\d{2})-(?<month>\d{1,2})$",
        RegexOptions.Compiled);

    private static readonly Regex SlashRegex = new(
        @"^(?<month>\d{1,2})\/(?<year>(?:19|20)\d{2})$",
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

        var text = value.Trim();

        if (CurrentRegex.IsMatch(text))
        {
            return CurrentKey;
        }

        var monthYear = MonthYearRegex.Match(text);
        if (monthYear.Success)
        {
            var month = MonthIndex(monthYear.Groups["month"].Value);
            var year = int.Parse(monthYear.Groups["year"].Value);
            return year * 12 + (month ?? 0);
        }

        var iso = IsoRegex.Match(text);
        if (iso.Success && TryReadMonth(iso.Groups["month"].Value, out var isoMonth))
        {
            return int.Parse(iso.Groups["year"].Value) * 12 + isoMonth;
        }

        var slash = SlashRegex.Match(text);
        if (slash.Success && TryReadMonth(slash.Groups["month"].Value, out var slashMonth))
        {
            return int.Parse(slash.Groups["year"].Value) * 12 + slashMonth;
        }

        var yearOnly = YearOnlyRegex.Match(text);
        if (yearOnly.Success)
        {
            return int.Parse(yearOnly.Groups["year"].Value) * 12;
        }

        var anyYear = AnyYearRegex.Match(text);
        if (anyYear.Success)
        {
            return int.Parse(anyYear.Groups["year"].Value) * 12;
        }

        return null;
    }

    /// <summary>
    /// Stable newest-first ordering. Items with a usable key come first (highest
    /// key first); items with no key sink to the end while keeping their original
    /// relative order.
    /// </summary>
    public static List<T> SortNewestFirst<T>(IEnumerable<T> items, Func<T, int?> selectKey)
    {
        return items
            .Select((item, index) => (item, index, key: selectKey(item)))
            .OrderBy(entry => entry.key.HasValue ? 0 : 1)
            .ThenByDescending(entry => entry.key ?? int.MinValue)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.item)
            .ToList();
    }

    private static bool TryReadMonth(string raw, out int month)
    {
        month = int.Parse(raw) - 1;
        return month is >= 0 and <= 11;
    }

    private static int? MonthIndex(string name)
    {
        var trimmed = name.Trim().ToLowerInvariant();
        if (trimmed.Length < 3)
        {
            return null;
        }

        var index = Array.IndexOf(MonthPrefixes, trimmed[..3]);
        return index == -1 ? null : index;
    }
}

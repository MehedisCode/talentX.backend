using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Normalizes the dates extracted for an education entry.
///
/// Two things happen here:
/// <list type="bullet">
///   <item>A single year is the completion/graduation year, so it is moved to
///   <c>endDate</c> (the AI used to always write it into <c>startDate</c>).</item>
///   <item>If the model returns a whole range in one field (e.g. "2016 - 2020"),
///   it is split across <c>startDate</c> and <c>endDate</c>.</item>
/// </list>
/// </summary>
public static class EducationDateNormalizer
{
    private static readonly Regex LoneYearRegex =
        new(@"^\d{4}\.?$", RegexOptions.Compiled);

    private static readonly Regex IsoDateRegex =
        new(@"^\d{4}-\d{1,2}(?:-\d{1,2})?$", RegexOptions.Compiled);

    private static readonly Regex RangeRegex =
        new(@"^(?<first>.+?)\s*(?:-|–|—|\bto\b)\s*(?<second>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] CompletionCues =
    [
        "expected", "graduated", "graduation", "class of", "completion", "completed"
    ];

    public static (string? StartDate, string? EndDate) Normalize(string? startDate, string? endDate)
    {
        var start = NormalizeValue(startDate);
        var end = NormalizeValue(endDate);

        // The model may return a full range in just one of the fields.
        if (!string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
        {
            var (first, second) = TrySplitRange(start);
            if (second is not null)
            {
                start = first;
                end = second;
            }
        }
        else if (string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end))
        {
            var (first, second) = TrySplitRange(end);
            if (second is not null)
            {
                start = first;
                end = second;
            }
        }

        if (string.IsNullOrWhiteSpace(start))
        {
            return (null, end);
        }

        // Only one date was provided: a lone year (or completion cue) is the
        // graduation year, not the start year.
        if (string.IsNullOrWhiteSpace(end))
        {
            return LooksLikeCompletion(start) ? (null, start) : (start, null);
        }

        // Both present but identical lone year -> treat it as completion only.
        if (LoneYearRegex.IsMatch(start) && string.Equals(start, end, StringComparison.OrdinalIgnoreCase))
        {
            return (null, end);
        }

        return (start, end);
    }

    private static (string? First, string? Second) TrySplitRange(string value)
    {
        if (IsoDateRegex.IsMatch(value))
        {
            return (value, null);
        }

        var match = RangeRegex.Match(value);
        if (!match.Success)
        {
            return (value, null);
        }

        var first = match.Groups["first"].Value.Trim();
        var second = match.Groups["second"].Value.Trim();

        if (first.Length == 0 || second.Length == 0)
        {
            return (value, null);
        }

        return (first, second);
    }

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool LooksLikeCompletion(string value)
    {
        if (LoneYearRegex.IsMatch(value))
        {
            return true;
        }

        var lowered = value.ToLowerInvariant();
        return CompletionCues.Any(cue => lowered.Contains(cue, StringComparison.Ordinal));
    }
}

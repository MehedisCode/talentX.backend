using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Normalizes the dates extracted for an education entry.
///
/// Resumes often list a single year for a degree (e.g. "2019"). The AI used to
/// always write that lone year into <c>startDate</c>, which is wrong: a single
/// year on an education entry is the completion/graduation year, so it belongs
/// in <c>endDate</c>. This normalizer moves a lone year (or an explicit
/// completion cue such as "Expected 2024") to <c>endDate</c>.
/// </summary>
public static class EducationDateNormalizer
{
    private static readonly Regex LoneYearRegex =
        new(@"^\d{4}\.?$", RegexOptions.Compiled);

    private static readonly string[] CompletionCues =
    [
        "expected", "graduated", "graduation", "class of", "completion", "completed"
    ];

    public static (string? StartDate, string? EndDate) Normalize(string? startDate, string? endDate)
    {
        var start = NormalizeValue(startDate);
        var end = NormalizeValue(endDate);

        if (string.IsNullOrWhiteSpace(start))
        {
            return (null, end);
        }

        if (!string.IsNullOrWhiteSpace(end))
        {
            return (start, end);
        }

        // Only one date was provided. A single year (or a completion cue) is the
        // graduation year, not the start year.
        if (LooksLikeCompletion(start))
        {
            return (null, start);
        }

        return (start, null);
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

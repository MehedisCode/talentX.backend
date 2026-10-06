using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Determines whether a work-experience entry is the candidate's current role.
///
/// The model is asked to set <c>isCurrent</c> and <c>endDate: "Present"</c> for
/// the current role, but it may phrase the end date differently ("Current",
/// "Now", "Ongoing", ...); this normalizes that so the UI can pre-check the
/// "I currently work here" checkbox.
///
/// A role the model flagged as current but which has NO dates at all is a
/// hallucinated classification (no date evidence exists — TX26-433); it is
/// treated as not current so the UI does not mark "Present".
/// </summary>
public static class WorkExperienceNormalizer
{
    private static readonly Regex CurrentPattern =
        new(@"\b(present|current|currently|now|ongoing|till date|to date)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsCurrent(bool? isCurrentFlag, string? endDate, string? startDate = null)
    {
        if (string.IsNullOrWhiteSpace(startDate) && string.IsNullOrWhiteSpace(endDate))
        {
            return false;
        }

        if (isCurrentFlag == true)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(endDate) && CurrentPattern.IsMatch(endDate);
    }

    public static string? NormalizeEndDate(string? endDate, bool isCurrent)
    {
        if (isCurrent)
        {
            return "Present";
        }

        return string.IsNullOrWhiteSpace(endDate) ? null : endDate.Trim();
    }
}

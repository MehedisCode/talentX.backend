using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

/// <summary>
/// Determines whether a work-experience entry is the candidate's current role.
///
/// The model is asked to set <c>isCurrent</c> and <c>endDate: "Present"</c> for
/// the current role, but it may phrase the end date differently ("Current",
/// "Now", "Ongoing", ...); this normalizes that so the UI can pre-check the
/// "I currently work here" checkbox.
/// </summary>
public static class WorkExperienceNormalizer
{
    private static readonly Regex CurrentPattern =
        new(@"\b(present|current|currently|now|ongoing|till date|to date)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsCurrent(bool? isCurrentFlag, string? endDate)
    {
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

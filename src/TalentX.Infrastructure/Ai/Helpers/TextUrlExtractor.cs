using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class TextUrlExtractor
{
    private static readonly Regex FullUrlRegex = new(
        @"(?:https?:\/\/|ftp:\/\/|mailto:)[^\s<>""'\[\]\(\)]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BareDomainRegex = new(
        @"\b(?:(?:www\.)?(?:linkedin\.com\/(?:in|company)\/[a-zA-Z0-9_\-\.]+|github\.com\/[a-zA-Z0-9_\-\.]+|gitlab\.com\/[a-zA-Z0-9_\-\.]+|[a-zA-Z0-9_\-\.]+\.(?:github|gitlab)\.io))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly char[] TrailingTrimChars = ['.', ',', ';', ':', ')', ']', '>', '"', '\''];

    public static IReadOnlyList<string> CombineAndDeduplicate(
        string plainText,
        IEnumerable<string> documentLinks)
    {
        var linkSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var link in documentLinks)
        {
            AddCleanLink(link, linkSet, result);
        }

        if (string.IsNullOrWhiteSpace(plainText))
            return result;

        foreach (Match match in FullUrlRegex.Matches(plainText))
        {
            AddCleanLink(match.Value, linkSet, result);
        }

        foreach (Match match in BareDomainRegex.Matches(plainText))
        {
            var raw = match.Value.TrimEnd(TrailingTrimChars);
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var fullUrl = raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                          raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? raw
                : $"https://{raw}";

            AddCleanLink(fullUrl, linkSet, result);
        }

        return result;
    }

    private static void AddCleanLink(string? rawUri, HashSet<string> set, List<string> list)
    {
        if (string.IsNullOrWhiteSpace(rawUri)) return;

        var clean = rawUri.Trim().TrimEnd(TrailingTrimChars);
        if (string.IsNullOrWhiteSpace(clean)) return;

        if (set.Add(clean))
        {
            list.Add(clean);
        }
    }
}

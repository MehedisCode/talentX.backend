using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class TextUrlExtractor
{
    // Curated TLD allowlist — keeps bare-domain detection precise enough to avoid
    // matching version numbers, file extensions, code tokens, etc.
    private static readonly string[] AllowedTlds =
    [
        "com", "org", "net", "edu", "gov", "mil", "int", "info", "biz",
        "io", "dev", "app", "ai", "co", "me", "xyz", "tech", "site", "online",
        "store", "shop", "blog", "cloud", "space", "live", "life", "world",
        "design", "art", "studio", "media", "agency", "digital", "software",
        "solutions", "services", "group", "pro", "name", "website", "page",
        "wiki", "zone", "ninja", "guru", "expert", "works", "tools", "press",
        "news", "network", "systems", "club", "vip", "top", "fun",
        "us", "uk", "ca", "au", "de", "fr", "es", "it", "nl", "se", "no", "fi",
        "dk", "ch", "at", "be", "ie", "nz", "sg", "my", "ph", "id", "pk", "bd",
        "in", "lk", "np", "jp", "cn", "kr", "hk", "tw", "br", "mx", "ar", "cl",
        "za", "ng", "ke", "eg", "tr", "ru", "pl", "pt", "gr", "cz", "ro", "hu", "ua"
    ];

    private static readonly string TldAlternation = string.Join(
        "|",
        AllowedTlds.OrderByDescending(tld => tld.Length));

    private static readonly Regex FullUrlRegex = new(
        @"(?:https?:\/\/|ftp:\/\/|mailto:)[^\s<>""'\[\]\(\)]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Bare host (optional subdomains / www.) + allowlisted TLD + optional path/port.
    // The lookbehind skips email domains and partially-consumed tokens; the
    // lookahead after the TLD stops us matching only part of a longer TLD.
    private static readonly Regex BareDomainRegex = new(
        @"(?<![@\w.-])(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:" + TldAlternation + @")(?![\w-])(?::\d+)?(?:/[^\s<>""'\[\]\(\)]*)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly char[] TrailingTrimChars = ['.', ',', ';', ':', ')', ']', '>', '"', '\'', '|'];

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

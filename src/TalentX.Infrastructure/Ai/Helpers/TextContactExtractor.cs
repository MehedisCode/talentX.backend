using System.Text.RegularExpressions;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class TextContactExtractor
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"(?:\+?\d{1,3}[\s.-]*)?\(?\d{2,4}\)?[\s.-]*\d{3,4}[\s.-]*\d{3,4}",
        RegexOptions.Compiled);

    public static string? ExtractEmail(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return null;

        var match = EmailRegex.Match(plainText);
        return match.Success ? match.Value.Trim() : null;
    }

    public static string? ExtractPhone(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return null;

        var match = PhoneRegex.Match(plainText);
        if (!match.Success) return null;

        var raw = Regex.Replace(match.Value.Trim(), @"\s+", " ");
        // Ignore matches that are too short (e.g. year range numbers)
        var digitsOnly = Regex.Replace(raw, @"\D", "");
        if (digitsOnly.Length < 7 || digitsOnly.Length > 15) return null;

        return raw;
    }
}

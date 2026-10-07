using TalentX.Application.Common.Exceptions;

namespace TalentX.Application.Features.Talents;

public static class TalentMediaStorageKeyGuard
{
    public static readonly string[] Categories = ["profile-pictures", "video-intros", "resumes", "company-logos", "job-applications"];

    public static string BuildStorageKey(string category, string userId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            userId.Contains('/') ||
            userId.Contains('\\') ||
            userId is "." or "..")
            throw new BadRequestException("UserId must not contain '/' or '\\' characters, and must not be '.' or '..'.");

        var sanitizedFileName = SanitizeFileNameSegment(fileName);

        if (sanitizedFileName is "." or "..")
            throw new BadRequestException("File name must not sanitize to '.' or '..'.");

        return $"{category}/{userId}/{sanitizedFileName}";
    }

    public static string SanitizeFileNameSegment(string fileName)
    {
        var value = fileName ?? string.Empty;
        var lastSeparatorIndex = value.LastIndexOfAny(['/', '\\']);

        return lastSeparatorIndex >= 0 ? value[(lastSeparatorIndex + 1)..] : value;
    }

    public static void EnsureOwnedByUser(string storageKey, string userId, string expectedCategory)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('\\'))
            throw new NotFoundException("The requested storage key was not found.");

        var segments = storageKey.Split('/');

        if (segments.Length < 2)
            throw new NotFoundException("The requested storage key was not found.");

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
                throw new NotFoundException("The requested storage key was not found.");
        }

        if (segments[0] != expectedCategory || segments[1] != userId)
            throw new NotFoundException("The requested storage key was not found.");
    }

    public static string ExtractOriginalFileName(string? storageKeyOrFileName, string fallback = "Resume.pdf")
    {
        if (string.IsNullOrWhiteSpace(storageKeyOrFileName))
            return fallback;

        var segment = SanitizeFileNameSegment(storageKeyOrFileName.Trim());

        if (string.IsNullOrWhiteSpace(segment) || segment is "." or "..")
            return fallback;

        var firstUnderscoreIndex = segment.IndexOf('_');
        if (firstUnderscoreIndex > 0 &&
            Guid.TryParse(segment[..firstUnderscoreIndex], out _))
        {
            var extractedName = segment[(firstUnderscoreIndex + 1)..];
            return string.IsNullOrWhiteSpace(extractedName) ? fallback : extractedName;
        }

        return segment;
    }
}

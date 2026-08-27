using TalentX.Application.Common.Exceptions;

namespace TalentX.Application.Features.Talents;

public static class TalentMediaStorageKeyGuard
{
    public static void EnsureOwnedByUser(string storageKey, string userId, string expectedCategory)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('\\'))
            throw new ForbiddenException("You do not own this storage key.");

        var segments = storageKey.Split('/');

        if (segments.Length < 2)
            throw new ForbiddenException("You do not own this storage key.");

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
                throw new ForbiddenException("You do not own this storage key.");
        }

        if (segments[0] != expectedCategory || segments[1] != userId)
            throw new ForbiddenException("You do not own this storage key.");
    }
}

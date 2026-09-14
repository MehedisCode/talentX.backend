using System.Collections.Concurrent;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public static class DeleteAccountOtpAttemptGuard
{
    public const int MaxFailedAttempts = 5;

    private static readonly ConcurrentDictionary<string, int> FailedAttempts = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsBlocked(string otpKey) =>
        FailedAttempts.TryGetValue(otpKey, out var count) && count >= MaxFailedAttempts;

    public static void RecordFailure(string otpKey) =>
        FailedAttempts.AddOrUpdate(otpKey, 1, (_, count) => count + 1);

    public static void Reset(string otpKey) =>
        FailedAttempts.TryRemove(otpKey, out _);
}

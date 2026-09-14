using System.Collections.Concurrent;

namespace TalentX.Application.Features.Auth.DeleteAccount;

/// <summary>
/// In-memory failed-attempt guard for the account-deletion OTP (per OTP key).
/// After <see cref="MaxFailedAttempts"/> consecutive failures the OTP is considered
/// burned: further verification attempts are refused and a new code must be requested.
/// Keyed by the purpose-qualified OTP key; reset on successful verification and when
/// a new code is issued. Acceptable as in-memory state for a single-instance API —
/// move to a distributed store when the API scales horizontally.
/// </summary>
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

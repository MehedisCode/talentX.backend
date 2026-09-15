namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Tracks failed OTP verification attempts per OTP key so a code cannot be
/// brute-forced. Implementations may be in-memory or shared (e.g. Redis).
/// </summary>
public interface IOtpAttemptTracker
{
    Task<bool> IsBlockedAsync(string otpKey, CancellationToken cancellationToken = default);

    Task RecordFailureAsync(string otpKey, CancellationToken cancellationToken = default);

    Task ResetAsync(string otpKey, CancellationToken cancellationToken = default);
}

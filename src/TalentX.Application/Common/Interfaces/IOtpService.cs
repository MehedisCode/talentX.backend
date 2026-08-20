namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service contract for generating and validating One-Time Passwords (OTP).
/// Completely separated from Identity and User authentication services.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a 6-digit numeric OTP code for the given key with an expiration time.
    /// </summary>
    /// <param name="key">The unique key or email address.</param>
    /// <param name="expiry">Optional expiration timespan (default: 10 minutes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated 6-digit numeric OTP code.</returns>
    Task<string> GenerateOtpAsync(
        string key,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Validates the provided OTP code against the key and invalidates it upon successful verification.
    /// </summary>
    /// <param name="key">The unique key or email address.</param>
    /// <param name="otpCode">The 6-digit OTP code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the OTP code is valid and active; otherwise false.</returns>
    Task<bool> VerifyOtpAsync(
        string key,
        string otpCode,
        CancellationToken cancellationToken = default
    );
}

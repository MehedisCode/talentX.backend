namespace TalentX.Application.Common.Constants;

/// <summary>
/// Shared policy for per-key OTP verification attempt limiting.
/// </summary>
public static class OtpAttemptPolicy
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);
}

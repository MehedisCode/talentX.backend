namespace TalentX.Application.Common.Constants;

public static class OtpAttemptPolicy
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TalentX.Infrastructure.RateLimiting.Policies;

internal static class AuthRateLimitPolicies
{
    internal static void Register(RateLimiterOptions options)
    {
        // POST /api/v1/auth/verify-email — Anonymous, IP-keyed
        options.AddPolicy("VerifyEmail", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetClientIp(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // POST /api/v1/auth/delete-account/send-otp — Authenticated, User-keyed.
        // Second defense layer behind the handler's 60s resend cooldown: caps the
        // email-bomb/SMTP-quota damage from a looping caller.
        options.AddPolicy("DeleteAccountOtp", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0
                }));
    }
}

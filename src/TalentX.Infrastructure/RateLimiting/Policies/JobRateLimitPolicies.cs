using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TalentX.Infrastructure.RateLimiting.Policies;

internal static class JobRateLimitPolicies
{
    internal static void Register(RateLimiterOptions options)
    {
        // POST /api/v1/jobs/{id}/apply — Anonymous, IP-keyed
        options.AddPolicy("JobApply", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetClientIp(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // POST /api/v1/jobs/application-resumes — Authenticated, User-keyed
        options.AddPolicy("UploadResume", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
}

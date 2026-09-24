using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TalentX.Infrastructure.RateLimiting.Policies;

internal static class InterviewPrepRateLimitPolicies
{
    internal static void Register(RateLimiterOptions options)
    {
        // POST interview preparation generation/regeneration — User-keyed, 10 per day
        options.AddPolicy("InterviewPrepGenerate", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromHours(24),
                    QueueLimit = 0
                }));
    }
}

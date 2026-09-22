using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TalentX.Infrastructure.RateLimiting.Policies;

internal static class EmployerRateLimitPolicies
{
    internal static void Register(RateLimiterOptions options)
    {
        // POST /api/v1/employers/jobs/{jobId}/match — Authenticated, User-keyed
        options.AddPolicy("RunMatching", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // GET /api/v1/employers/jobs/{jobId}/candidates — Authenticated, User-keyed
        options.AddPolicy("GetCandidates", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // GET /api/v1/employers/candidates/{id}/summary — Authenticated, User-keyed
        options.AddPolicy("CandidateSummary", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 15,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using TalentX.Application.Features.InterviewPreparation;

namespace TalentX.Infrastructure.RateLimiting.Policies;

internal static class InterviewPrepRateLimitPolicies
{
    internal static void Register(RateLimiterOptions options, IConfiguration configuration)
    {
        var settings =
            configuration.GetSection(InterviewPreparationSettings.SectionName)
                .Get<InterviewPreparationSettings>()
            ?? new InterviewPreparationSettings();

        options.AddPolicy("InterviewPrepGenerate", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitingStartup.GetUserId(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.RateLimitPermits,
                    Window = TimeSpan.FromHours(settings.RateLimitWindowHours),
                    QueueLimit = 0
                }));
    }
}

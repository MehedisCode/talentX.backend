using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.RateLimiting.Policies;

namespace TalentX.Infrastructure.RateLimiting;

public static class RateLimitingStartup
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // ── Custom 429 JSON response (standard error structure) ──
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue
                    : TimeSpan.FromSeconds(60);

                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ErrorResponseDto(
                        new UserResponse("Too many requests. Please try again later."),
                        new DevResponse(
                            "Rate limit exceeded.",
                            Details: $"Retry after {(int)retryAfter.TotalSeconds} seconds.",
                            ExceptionType: "RateLimitExceeded")),
                    cancellationToken);
            };

            // ── Register domain-specific policies ──
            AuthRateLimitPolicies.Register(options);
            JobRateLimitPolicies.Register(options);
            EmployerRateLimitPolicies.Register(options);
            InterviewPrepRateLimitPolicies.Register(options, configuration);
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }

    // ── Shared helpers (used by policy files) ──

    internal static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    internal static string GetUserId(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? GetClientIp(context);
}

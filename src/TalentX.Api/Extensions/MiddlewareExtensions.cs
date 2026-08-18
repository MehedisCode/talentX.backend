using TalentX.API.Middlewares;

namespace TalentX.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware
    (
        this IApplicationBuilder app
    )
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    public static IApplicationBuilder UseRequestLoggingMiddleware(
        this IApplicationBuilder app
    )
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
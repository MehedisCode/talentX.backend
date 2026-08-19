using Microsoft.AspNetCore.Builder;
using TalentX.Infrastructure.Middlewares;

namespace TalentX.Infrastructure.Middlewares;

public static class MiddlewareStartup
{
    public static IApplicationBuilder UseInfrastructureMiddlewares(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        return app;
    }
}

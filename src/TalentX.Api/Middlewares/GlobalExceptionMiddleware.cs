using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;

namespace TalentX.API.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
            await HandleExceptionAsync(
                context,
                exception
            );
        }
    }

    private static async Task HandleExceptionAsync
    (
        HttpContext context,
        Exception exception
    )
    {
        context.Response.ContentType = "application/json";

        object response = new { Message = exception.Message };

        switch (exception)
        {
            case FluentValidation.ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                response = new
                {
                    Message = "Validation Failed",
                    Errors = validationException.Errors
                        .Select(x => x.ErrorMessage)
                        .ToList()
                };
                break;

            case ConflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                break;

            case ForbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                break;

            case NotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case UnauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ValidationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
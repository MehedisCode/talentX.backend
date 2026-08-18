using System.Net;
using FluentValidation;
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected before the response completed - nothing to write.
            _logger.LogInformation(
                "Request {Method} {Path} was cancelled by the client",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "Exception occurred after response had already started for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    Message = "Validation Failed",
                    Errors = validationException.Errors
                        .Select(x => x.ErrorMessage)
                        .ToList()
                };
                _logger.LogWarning(
                    exception,
                    "Validation failed for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;

            case AppException appException:
                context.Response.StatusCode = (int)appException.StatusCode;
                response = new { Message = appException.Message };
                _logger.LogWarning(
                    exception,
                    "Handled {ExceptionType} for {Method} {Path}: {Message}",
                    exception.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    exception.Message);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new { Message = "An unexpected error occurred." };
                _logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;
        }

        await context.Response.WriteAsJsonAsync(response);
    }
}
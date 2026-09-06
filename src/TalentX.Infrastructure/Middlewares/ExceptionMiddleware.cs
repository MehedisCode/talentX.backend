using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.Middlewares;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
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

        var isNonProduction = _environment.IsDevelopment() || _environment.IsEnvironment("Staging");
        ErrorResponseDto response;

        switch (exception)
        {
            case FluentValidation.ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationErrors = validationException.Errors
                    .Select(error => error.ErrorMessage)
                    .ToList();
                var validationUserResponse = new UserResponse("Validation Failed", ResourceId: null, Errors: validationErrors);
                var validationDevResponse = new DevResponse(
                    "Validation Failed",
                    Details: string.Join("; ", validationErrors),
                    ExceptionType: nameof(FluentValidation.ValidationException),
                    StackTrace: isNonProduction ? validationException.StackTrace : null);
                response = new ErrorResponseDto(validationUserResponse, validationDevResponse);
                _logger.LogWarning(
                    exception,
                    "Validation failed for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;

            case AppException appException:
                context.Response.StatusCode = (int)appException.StatusCode;
                var appUserResponse = new UserResponse(appException.Message, ResourceId: appException.ResourceId);
                var appDevResponse = new DevResponse(
                    appException.Message,
                    Details: isNonProduction ? appException.ToString() : null,
                    ExceptionType: appException.GetType().Name,
                    StackTrace: isNonProduction ? appException.StackTrace : null);
                response = new ErrorResponseDto(appUserResponse, appDevResponse);
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
                var internalUserResponse = new UserResponse("An unexpected error occurred.");
                var internalDevResponse = new DevResponse(
                    isNonProduction ? exception.Message : "An internal server error occurred.",
                    Details: isNonProduction ? exception.ToString() : null,
                    ExceptionType: isNonProduction ? exception.GetType().Name : null,
                    StackTrace: isNonProduction ? exception.StackTrace : null);
                response = new ErrorResponseDto(internalUserResponse, internalDevResponse);
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
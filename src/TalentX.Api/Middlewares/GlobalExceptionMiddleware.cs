using System.Net;
using System.Text.Json;
using TalentX.Application.Common.Exceptions;

namespace TalentX.API.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(
        RequestDelegate next
    )
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch(Exception exception)
        {
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

        object response = new { Message = exception.Message};

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
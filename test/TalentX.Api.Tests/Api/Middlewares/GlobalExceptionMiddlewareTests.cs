using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using TalentX.API.Middlewares;
using TalentX.Application.Common.Exceptions;

namespace TalentX.Api.Tests.Api.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<(HttpContext context, JsonDocument body)> InvokeWithAsync(RequestDelegate next)
    {
        var middleware = new GlobalExceptionMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var text = await reader.ReadToEndAsync();
        return (context, JsonDocument.Parse(text));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_DoesNotTouchTheResponse()
    {
        var context = new DefaultHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_SetsResponseContentTypeToJson()
    {
        var (context, _) = await InvokeWithAsync(_ => throw new NotFoundException("missing"));

        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithFluentValidationValidationException_Returns400WithErrorList()
    {
        var failures = new[] { new ValidationFailure("Title", "Title is required.") };
        var (context, body) = await InvokeWithAsync(_ => throw new FluentValidation.ValidationException(failures));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("Message").GetString().Should().Be("Validation Failed");
        body.RootElement.GetProperty("Errors").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("Title is required.");
    }

    [Fact]
    public async Task InvokeAsync_WithConflictException_Returns409()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new ConflictException("already exists"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        body.RootElement.GetProperty("Message").GetString().Should().Be("already exists");
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenException_Returns403()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new ForbiddenException());

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        body.RootElement.GetProperty("Message").GetString().Should().Be("You are not allowed to perform this action.");
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new NotFoundException("Job 'x' was not found."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        body.RootElement.GetProperty("Message").GetString().Should().Be("Job 'x' was not found.");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_Returns401()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new UnauthorizedException("bad token"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        body.RootElement.GetProperty("Message").GetString().Should().Be("bad token");
    }

    [Fact]
    public async Task InvokeAsync_WithCustomValidationException_Returns400()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new ValidationException("Only draft jobs can be published."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("Message").GetString().Should().Be("Only draft jobs can be published.");
    }

    [Fact]
    public async Task InvokeAsync_WithAnUnrecognizedException_Returns500()
    {
        var (context, body) = await InvokeWithAsync(_ => throw new InvalidOperationException("unexpected"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        body.RootElement.GetProperty("Message").GetString().Should().Be("unexpected");
    }
}

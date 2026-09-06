using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Infrastructure.Middlewares;
using Xunit;

namespace TalentX.Api.Tests.Api.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<(HttpContext context, JsonDocument body)> InvokeWithAsync(
        RequestDelegate next,
        IHostEnvironment? environment = null)
    {
        var loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
        var defaultEnvironmentMock = new Mock<IHostEnvironment>();
        defaultEnvironmentMock
            .SetupGet(hostEnvironment => hostEnvironment.EnvironmentName)
            .Returns(Environments.Development);

        var middleware = new ExceptionMiddleware(next, loggerMock.Object, environment ?? defaultEnvironmentMock.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();
        return (context, JsonDocument.Parse(responseText));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_DoesNotTouchTheResponse()
    {
        var context = new DefaultHttpContext();
        var loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock
            .SetupGet(hostEnvironment => hostEnvironment.EnvironmentName)
            .Returns(Environments.Development);
        var middleware = new ExceptionMiddleware(_ => Task.CompletedTask, loggerMock.Object, environmentMock.Object);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_SetsResponseContentTypeToJson()
    {
        var (context, _) = await InvokeWithAsync(_ => throw new NotFoundException("missing"));

        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithFluentValidationValidationException_Returns400WithErrorList()
    {
        var failures = new[] { new ValidationFailure("Title", "Title is required.") };
        var (context, responseBody) = await InvokeWithAsync(_ => throw new FluentValidation.ValidationException(failures));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("Validation Failed");
        responseBody.RootElement.GetProperty("userResponse").GetProperty("errors").EnumerateArray()
            .Select(errorElement => errorElement.GetString()).Should().Contain("Title is required.");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithConflictException_Returns409()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new ConflictException("already exists"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("already exists");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenException_Returns403()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new ForbiddenException("You are not allowed to perform this action."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("You are not allowed to perform this action.");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_Returns404()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new NotFoundException("Job 'x' was not found."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("Job 'x' was not found.");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_Returns401()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new UnauthorizedException("bad token"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("bad token");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithCustomValidationException_Returns400()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new ValidationException("Only draft jobs can be published."));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("Only draft jobs can be published.");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithAnUnrecognizedException_Returns500()
    {
        var (context, responseBody) = await InvokeWithAsync(_ => throw new InvalidOperationException("unexpected"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        responseBody.RootElement.GetProperty("userResponse").GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        responseBody.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_WithResourceId_PopulatesUserResponseAndDevResponse()
    {
        var (context, responseBody) = await InvokeWithAsync(_ =>
            throw new NotFoundException("Job with ID 'abc' was not found.", "abc"));

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var rootElement = responseBody.RootElement;
        var userResponse = rootElement.GetProperty("userResponse");
        var devResponse = rootElement.GetProperty("devResponse");

        userResponse.GetProperty("message").GetString().Should().Be("Job with ID 'abc' was not found.");
        userResponse.GetProperty("resourceId").GetString().Should().Be("abc");
        devResponse.GetProperty("exceptionType").GetString().Should().Be("NotFoundException");
        rootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_WithoutResourceId_HasNullResourceId()
    {
        var (_, responseBody) = await InvokeWithAsync(_ =>
            throw new NotFoundException("Job was not found."));

        var userResponse = responseBody.RootElement.GetProperty("userResponse");
        userResponse.GetProperty("resourceId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_RedactsDevResponseDetails()
    {
        var productionEnvironmentMock = new Mock<IHostEnvironment>();
        productionEnvironmentMock
            .SetupGet(hostEnvironment => hostEnvironment.EnvironmentName)
            .Returns(Environments.Production);

        var (context, responseBody) = await InvokeWithAsync(
            _ => throw new InvalidOperationException("secret database error"),
            productionEnvironmentMock.Object);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var rootElement = responseBody.RootElement;
        var userResponse = rootElement.GetProperty("userResponse");
        var devResponse = rootElement.GetProperty("devResponse");

        userResponse.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        devResponse.GetProperty("message").GetString().Should().Be("An internal server error occurred.");
        devResponse.GetProperty("details").ValueKind.Should().Be(JsonValueKind.Null);
        devResponse.GetProperty("exceptionType").ValueKind.Should().Be(JsonValueKind.Null);
        devResponse.GetProperty("stackTrace").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task InvokeAsync_WithAppException_InProduction_RedactsDevResponseDetailsAndStackTrace()
    {
        var productionEnvironmentMock = new Mock<IHostEnvironment>();
        productionEnvironmentMock
            .SetupGet(hostEnvironment => hostEnvironment.EnvironmentName)
            .Returns(Environments.Production);

        var (context, responseBody) = await InvokeWithAsync(
            _ => throw new NotFoundException("Job with ID 'job-123' was not found.", "job-123"),
            productionEnvironmentMock.Object);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var rootElement = responseBody.RootElement;
        var userResponse = rootElement.GetProperty("userResponse");
        var devResponse = rootElement.GetProperty("devResponse");

        devResponse.GetProperty("details").ValueKind.Should().Be(JsonValueKind.Null);
        devResponse.GetProperty("stackTrace").ValueKind.Should().Be(JsonValueKind.Null);
        userResponse.GetProperty("message").GetString().Should().Be("Job with ID 'job-123' was not found.");
        userResponse.GetProperty("resourceId").GetString().Should().Be("job-123");
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_IncludesDevResponseDetails()
    {
        var (_, responseBody) = await InvokeWithAsync(_ =>
            throw new InvalidOperationException("debug info"));

        var devResponse = responseBody.RootElement.GetProperty("devResponse");
        devResponse.GetProperty("details").GetString().Should().NotBeNullOrWhiteSpace();
        devResponse.GetProperty("stackTrace").GetString().Should().NotBeNullOrWhiteSpace();
    }
}

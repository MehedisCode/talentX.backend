using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Infrastructure.Middlewares;
using Xunit;

namespace TalentX.Api.Tests.Api.Middlewares;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new RequestLoggingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_LogsOnceAfterTheRequestCompletes()
    {
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        RequestDelegate next = context =>
        {
            context.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };
        var middleware = new RequestLoggingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/Jobs";

        await middleware.InvokeAsync(context);

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_PropagatesAndDoesNotLog()
    {
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new RequestLoggingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext();

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        logger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}

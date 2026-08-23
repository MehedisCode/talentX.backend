using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Behaviours;

namespace TalentX.Api.Tests.Application.Common.Behaviours;

public class LoggingBehaviourTests
{
    public class SampleRequest : IRequest<string>
    {
    }

    [Fact]
    public async Task Handle_CallsNextAndReturnsItsResult()
    {
        var logger = new Mock<ILogger<LoggingBehaviour<SampleRequest, string>>>();
        var behaviour = new LoggingBehaviour<SampleRequest, string>(logger.Object);
        RequestHandlerDelegate<string> next = (_) => Task.FromResult("result");

        var result = await behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        result.Should().Be("result");
    }

    [Fact]
    public async Task Handle_LogsBeforeAndAfterInvokingNext()
    {
        var logger = new Mock<ILogger<LoggingBehaviour<SampleRequest, string>>>();
        var behaviour = new LoggingBehaviour<SampleRequest, string>(logger.Object);
        RequestHandlerDelegate<string> next = (_) => Task.FromResult("result");

        await behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_WhenNextThrows_PropagatesTheException()
    {
        var logger = new Mock<ILogger<LoggingBehaviour<SampleRequest, string>>>();
        var behaviour = new LoggingBehaviour<SampleRequest, string>(logger.Object);
        RequestHandlerDelegate<string> next = (_) => throw new InvalidOperationException("boom");

        var act = () => behaviour.Handle(new SampleRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }
}

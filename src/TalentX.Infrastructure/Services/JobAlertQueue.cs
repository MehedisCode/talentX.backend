using System.Threading.Channels;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Services;

public sealed class JobAlertQueue : IJobAlertQueue
{
    private readonly Channel<Guid> _channel;

    public JobAlertQueue()
    {
        _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
    }

    public ValueTask EnqueuePublishedJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

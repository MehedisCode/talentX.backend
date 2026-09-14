namespace TalentX.Application.Common.Interfaces;

public interface IJobAlertQueue
{
    ValueTask EnqueuePublishedJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
}

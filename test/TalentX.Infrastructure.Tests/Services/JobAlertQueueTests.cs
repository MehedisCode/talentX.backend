using FluentAssertions;
using TalentX.Infrastructure.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Services;

public sealed class JobAlertQueueTests
{
    [Fact]
    public async Task EnqueuePublishedJobAsync_And_ReadAllAsync_ReadsEnqueuedJobIds()
    {
        var queue = new JobAlertQueue();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();

        await queue.EnqueuePublishedJobAsync(jobId1);
        await queue.EnqueuePublishedJobAsync(jobId2);

        using var cts = new CancellationTokenSource();
        var readJobIds = new List<Guid>();

        await foreach (var jobId in queue.ReadAllAsync(cts.Token))
        {
            readJobIds.Add(jobId);
            if (readJobIds.Count == 2)
            {
                break;
            }
        }

        readJobIds.Should().ContainInOrder(jobId1, jobId2);
    }
}

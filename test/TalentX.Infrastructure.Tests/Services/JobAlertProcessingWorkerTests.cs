using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Services;

public sealed class JobAlertProcessingWorkerTests
{
    private readonly Mock<IJobAlertQueue> _queue = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<IJobAlertMatchingService> _matchingService = new();
    private readonly Mock<ILogger<JobAlertProcessingWorker>> _logger = new();

    public JobAlertProcessingWorkerTests()
    {
        _scopeFactory.Setup(factory => factory.CreateScope()).Returns(_scope.Object);
        _scope.SetupGet(scope => scope.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(provider => provider.GetService(typeof(IJobAlertMatchingService)))
            .Returns(_matchingService.Object);
    }

    private static async IAsyncEnumerable<Guid> CreateJobStream(params Guid[] jobIds)
    {
        foreach (var jobId in jobIds)
        {
            yield return jobId;
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_ReadsJobsFromQueueAndInvokesMatchingService()
    {
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();

        _queue.Setup(q => q.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns(CreateJobStream(jobId1, jobId2));

        var worker = new JobAlertProcessingWorker(_queue.Object, _scopeFactory.Object, _logger.Object);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(50);
        await worker.StopAsync(cts.Token);

        _matchingService.Verify(s => s.ProcessAlertsForJobAsync(jobId1, It.IsAny<CancellationToken>()), Times.Once);
        _matchingService.Verify(s => s.ProcessAlertsForJobAsync(jobId2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMatchingServiceThrows_LogsErrorAndContinues()
    {
        var failingJobId = Guid.NewGuid();
        var succeedingJobId = Guid.NewGuid();

        _queue.Setup(q => q.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns(CreateJobStream(failingJobId, succeedingJobId));

        _matchingService.Setup(s => s.ProcessAlertsForJobAsync(failingJobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var worker = new JobAlertProcessingWorker(_queue.Object, _scopeFactory.Object, _logger.Object);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(50);
        await worker.StopAsync(cts.Token);

        _matchingService.Verify(s => s.ProcessAlertsForJobAsync(failingJobId, It.IsAny<CancellationToken>()), Times.Once);
        _matchingService.Verify(s => s.ProcessAlertsForJobAsync(succeedingJobId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

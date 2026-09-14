using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Services;

public sealed class JobAlertProcessingWorker : BackgroundService
{
    private readonly IJobAlertQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobAlertProcessingWorker> _logger;

    public JobAlertProcessingWorker(
        IJobAlertQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<JobAlertProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobAlertProcessingWorker started and listening for published jobs");

        try
        {
            await foreach (var jobId in _queue.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var matchingService = scope.ServiceProvider.GetRequiredService<IJobAlertMatchingService>();

                try
                {
                    await matchingService.ProcessAlertsForJobAsync(jobId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job alerts for job {JobId}", jobId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown on cancellation
        }

        _logger.LogInformation("JobAlertProcessingWorker stopped");
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Mailing;

public sealed class EmailDispatchBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDispatchBackgroundWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

    public EmailDispatchBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailDispatchBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OUTBOX] EmailDispatchBackgroundWorker started. Polling every {Interval}s.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OUTBOX] Unexpected error in EmailDispatchBackgroundWorker execution loop.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("[OUTBOX] EmailDispatchBackgroundWorker stopped.");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var pendingLogs = await unitOfWork.EmailDispatchLogs.ClaimPendingOrRetryableLogsAsync(10, stoppingToken);
        if (pendingLogs.Count == 0)
        {
            return;
        }

        _logger.LogInformation("[OUTBOX] Processing {Count} queued email logs...", pendingLogs.Count);

        foreach (var log in pendingLogs)
        {
            try
            {
                await emailService.DispatchDirectAsync(
                    log.RecipientEmail,
                    log.RecipientName,
                    log.Subject,
                    log.HtmlBody,
                    stoppingToken);

                log.MarkSent();
                _logger.LogInformation("[OUTBOX] Successfully sent email LogId {LogId} to {Email}.", log.Id, log.RecipientEmail);
            }
            catch (Exception ex)
            {
                var retryDelay = GetExponentialBackoffDelay(log.AttemptCount);
                log.MarkFailed(ex.Message, retryDelay);

                _logger.LogWarning(
                    ex,
                    "[OUTBOX] Failed to send email LogId {LogId} (Attempt {Attempt}/{Max}). Next retry in {Delay} mins.",
                    log.Id, log.AttemptCount, log.MaxAttempts, retryDelay.TotalMinutes);
            }

            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
    }

    private static TimeSpan GetExponentialBackoffDelay(int attemptCount)
    {
        return attemptCount switch
        {
            1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            4 => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(2)
        };
    }
}

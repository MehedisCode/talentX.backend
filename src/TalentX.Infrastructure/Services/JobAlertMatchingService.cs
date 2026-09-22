using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobAlerts.Constants;
using TalentX.Application.Features.JobAlerts.Extensions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Mailing;

namespace TalentX.Infrastructure.Services;

public sealed class JobAlertMatchingService : IJobAlertMatchingService
{
    private static readonly JsonSerializerOptions NotificationMetadataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<JobAlertMatchingService> _logger;

    public JobAlertMatchingService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<JobAlertMatchingService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessAlertsForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing job alerts for job {JobId}", jobId);

        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null || job.IsDeleted || job.Status != JobStatus.Published)
        {
            _logger.LogInformation("Job {JobId} is null, deleted, or not in published status. Skipping alert processing", jobId);
            return;
        }

        var activeAlerts = await _unitOfWork.JobAlerts.GetAllActiveAlertsAsync(cancellationToken);
        if (activeAlerts.Count == 0)
        {
            _logger.LogInformation("No active job alerts found. Skipping alert processing for job {JobId}", jobId);
            return;
        }

        var matchingAlerts = activeAlerts
            .Where(alert => alert.TalentId != job.EmployerId && alert.MatchesJob(job))
            .ToList();

        if (matchingAlerts.Count == 0)
        {
            _logger.LogInformation("No active job alerts matched job {JobId}", jobId);
            return;
        }

        var alertsByTalent = matchingAlerts
            .GroupBy(alert => alert.TalentId)
            .ToList();

        var candidateIds = alertsByTalent.Select(group => group.Key).ToList();
        var alreadyNotifiedTalentIds = await _unitOfWork.JobAlertNotificationHistories
            .GetNotifiedTalentIdsAsync(job.Id, candidateIds, cancellationToken);

        var emailsQueuedCount = 0;
        var frontendUrl = ResolveFrontendUrl();
        var jobUrl = $"{frontendUrl}/jobs/{job.Id}";
        var pendingNotifications = new List<(string TalentId, string Title, string Body, string MetadataJson)>();

        foreach (var candidateGroup in alertsByTalent)
        {
            var talentId = candidateGroup.Key;
            if (alreadyNotifiedTalentIds.Contains(talentId))
            {
                _logger.LogInformation("Talent {TalentId} has already been notified for job {JobId}. Skipping", talentId, job.Id);
                continue;
            }

            var preference = await _unitOfWork.TalentNotificationPreferences.GetByTalentIdAsync(talentId, cancellationToken);
            if (preference != null && !preference.JobAlertsEmailEnabled)
            {
                _logger.LogInformation("Talent {TalentId} has job alert emails disabled in preferences. Skipping", talentId);
                continue;
            }

            var talent = await _unitOfWork.Talents.GetByUserIdAsync(talentId, cancellationToken);
            string? email = talent?.Email;
            string? name = talent?.Name;

            if (string.IsNullOrWhiteSpace(email))
            {
                var user = await _unitOfWork.Users.GetByUserIdAsync(talentId);
                email = user?.Email;
                name = user?.Name;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Unable to resolve email address for talent {TalentId}. Skipping alert notification", talentId);
                continue;
            }

            var matchedAlert = candidateGroup.First();
            var recipientName = !string.IsNullOrWhiteSpace(name) ? name : JobAlertConstants.DefaultCandidateFallbackName;

            var history = new JobAlertNotificationHistory(talentId, job.Id, matchedAlert.Id);
            await _unitOfWork.JobAlertNotificationHistories.AddAsync(history, cancellationToken);

            var emailParameters = new RenderJobAlertEmailParameters(
                recipientName,
                job.Title,
                job.CompanyName,
                job.Location,
                jobUrl,
                matchedAlert.Name);

            var htmlBody = await _emailService.RenderJobAlertEmailAsync(emailParameters, cancellationToken);
            var subject = string.Format(EmailConstants.Subjects.JobAlert, job.Title, job.CompanyName);

            var emailDispatchLog = new EmailDispatchLog(
                email,
                recipientName,
                subject,
                htmlBody,
                EmailConstants.Kinds.JobAlert);

            await _unitOfWork.EmailDispatchLogs.AddAsync(emailDispatchLog, cancellationToken);
            emailsQueuedCount++;

            var metadataJson = JsonSerializer.Serialize(
                new
                {
                    jobId = job.Id,
                    keyword = matchedAlert.Title,
                    location = matchedAlert.Location,
                    employmentType = matchedAlert.EmploymentType,
                    experienceLevel = matchedAlert.ExperienceLevel,
                    workArrangement = matchedAlert.WorkArrangement
                },
                NotificationMetadataSerializerOptions);

            pendingNotifications.Add((
                talentId,
                $"New job matches your alert: {job.Title}",
                $"{job.Title} at {job.CompanyName} ({job.Location}) matches your alert \"{matchedAlert.Name}\".",
                metadataJson));
        }

        if (emailsQueuedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully queued {Count} job alert email(s) for job {JobId}", emailsQueuedCount, job.Id);
        }

        foreach (var pendingNotification in pendingNotifications)
        {
            try
            {
                var notificationParameters = new PublishNotificationParameters(
                    pendingNotification.TalentId,
                    NotificationType.JobAlertMatch,
                    pendingNotification.Title,
                    pendingNotification.Body,
                    pendingNotification.MetadataJson);

                await _notificationService.PublishAsync(notificationParameters, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to publish job alert notification for talent {TalentId} and job {JobId}",
                    pendingNotification.TalentId,
                    job.Id);
            }
        }
    }

    private static string ResolveFrontendUrl()
    {
        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            var firstOrigin = corsOrigins
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstOrigin))
            {
                return firstOrigin.TrimEnd('/');
            }
        }

        return JobAlertConstants.DefaultFrontendBaseUrl;
    }
}

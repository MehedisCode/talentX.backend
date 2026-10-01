using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Jobs.Moderation;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.ReactivateJob;

public sealed class ReactivateJobCommandHandler : IRequestHandler<ReactivateJobCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IJobAlertQueue _jobAlertQueue;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReactivateJobCommandHandler> _logger;

    public ReactivateJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IJobAlertQueue jobAlertQueue,
        INotificationService notificationService,
        ILogger<ReactivateJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _jobAlertQueue = jobAlertQueue;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<StatusUpdateResponseDto> Handle(ReactivateJobCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to reactivate a job.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to reactivate a job.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("Job", request.JobId);

        var fromStatus = job.Status;

        try
        {
            // Also refuses an expired deadline: the employer must extend it before the job
            // can go live again.
            job.Reactivate();
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        await _unitOfWork.JobModerationHistories.AddAsync(
            JobModerationHistory.Create(
                job.Id,
                JobModerationAction.Reactivated,
                fromStatus,
                job.Status,
                actorUserId,
                actorEmail),
            cancellationToken);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Reactivated job '{job.Title}'"),
            cancellationToken);

        await QueueEmployerEmailAsync(job, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishEmployerNotificationAsync(job, cancellationToken);

        // After the save, and best-effort: the job is public again so matching talents should
        // be alerted, but a queue failure must not fail an action that already committed.
        // JobAlertNotificationHistories prevents re-notifying anyone alerted before.
        try
        {
            await _jobAlertQueue.EnqueuePublishedJobAsync(job.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to enqueue reactivated job {JobId} for job alerts", job.Id);
        }

        return new StatusUpdateResponseDto("Job reactivated.");
    }

    // Best-effort, like the alert queue: the reactivation has already committed.
    private async Task PublishEmployerNotificationAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    job.EmployerId,
                    NotificationType.JobReactivated,
                    "Job Reactivated",
                    $"Your job '{job.Title}' is live again.",
                    JsonSerializer.Serialize(new { jobId = job.Id, jobTitle = job.Title })),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to publish in-app notification for reactivated job {JobId}",
                job.Id);
        }
    }

    private async Task QueueEmployerEmailAsync(Job job, CancellationToken cancellationToken)
    {
        // Job.EmployerId holds the login user id, which matches User.UserId, not User.Id.
        var employer = await _unitOfWork.Users.GetByUserIdAsync(job.EmployerId);
        if (employer is null)
        {
            _logger.LogWarning(
                "Reactivated job {JobId} but no user record exists for employer {EmployerId}; no email queued.",
                job.Id,
                job.EmployerId);
            return;
        }

        var htmlBody = await _emailService.RenderJobReactivatedEmailAsync(
            employer.Name,
            job.Title,
            ModerationLinks.ViewJob(job.Id),
            cancellationToken);

        await _unitOfWork.EmailDispatchLogs.AddAsync(
            new EmailDispatchLog(
                employer.Email,
                employer.Name,
                $"Your job \"{job.Title}\" is live again — TalentX",
                htmlBody,
                "JobReactivated"),
            cancellationToken);
    }
}

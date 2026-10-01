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

namespace TalentX.Application.Features.Jobs.Commands.DeactivateJob;

public sealed class DeactivateJobCommandHandler : IRequestHandler<DeactivateJobCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DeactivateJobCommandHandler> _logger;

    public DeactivateJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<DeactivateJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<StatusUpdateResponseDto> Handle(DeactivateJobCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to deactivate a job.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to deactivate a job.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("Job", request.JobId);

        var details = request.Details.Trim();
        var fromStatus = job.Status;

        try
        {
            job.Deactivate(request.Reason, details);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var reasonLabel = DeactivationReasonLabels.ToLabel(request.Reason);

        await _unitOfWork.JobModerationHistories.AddAsync(
            JobModerationHistory.Create(
                job.Id,
                JobModerationAction.Deactivated,
                fromStatus,
                job.Status,
                actorUserId,
                actorEmail,
                request.Reason,
                details),
            cancellationToken);

        // AuditLogSaveChangesInterceptor only records a generic "Update · Job"; this is the
        // human-readable line shown on the admin Audit Log page.
        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Deactivated job '{job.Title}' ({reasonLabel})"),
            cancellationToken);

        await QueueEmployerEmailAsync(job, reasonLabel, details, cancellationToken);

        // One save: status, history, audit log and queued email all commit together, so a job
        // can never end up deactivated without its employer being notified, or vice versa.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishEmployerNotificationAsync(job, reasonLabel, cancellationToken);

        return new StatusUpdateResponseDto("Job deactivated.");
    }

    // After the save, and best-effort: PublishAsync commits on its own, so a failure here must
    // not fail a deactivation that already went through. The email still reaches the employer.
    private async Task PublishEmployerNotificationAsync(
        Job job,
        string reasonLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    job.EmployerId,
                    NotificationType.JobDeactivated,
                    "Job Deactivated",
                    $"Your job '{job.Title}' was deactivated. Reason: {reasonLabel}.",
                    JsonSerializer.Serialize(new { jobId = job.Id, jobTitle = job.Title })),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to publish in-app notification for deactivated job {JobId}",
                job.Id);
        }
    }

    private async Task QueueEmployerEmailAsync(
        Job job,
        string reasonLabel,
        string details,
        CancellationToken cancellationToken)
    {
        // Job.EmployerId holds the login user id, which matches User.UserId, not User.Id.
        var employer = await _unitOfWork.Users.GetByUserIdAsync(job.EmployerId);
        if (employer is null)
        {
            // A missing user record must not block moderation; the job still comes down.
            _logger.LogWarning(
                "Deactivated job {JobId} but no user record exists for employer {EmployerId}; no email queued.",
                job.Id,
                job.EmployerId);
            return;
        }

        var htmlBody = await _emailService.RenderJobDeactivatedEmailAsync(
            new RenderJobDeactivatedEmailParameters(
                employer.Name,
                job.Title,
                reasonLabel,
                details,
                ModerationLinks.EditJob(job.Id)),
            cancellationToken);

        // Queued, not sent: EmailDispatchBackgroundWorker delivers it with retries, so an SMTP
        // outage can never fail or roll back the deactivation.
        await _unitOfWork.EmailDispatchLogs.AddAsync(
            new EmailDispatchLog(
                employer.Email,
                employer.Name,
                $"Your job \"{job.Title}\" has been deactivated — TalentX",
                htmlBody,
                "JobDeactivated"),
            cancellationToken);
    }
}

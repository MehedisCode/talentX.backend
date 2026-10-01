using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Commands.ResubmitJobForReview;

public sealed class ResubmitJobForReviewCommandHandler : IRequestHandler<ResubmitJobForReviewCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ResubmitJobForReviewCommandHandler> _logger;

    public ResubmitJobForReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ILogger<ResubmitJobForReviewCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResubmitJobForReviewCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to resubmit a job.");
        var employerEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to resubmit a job.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Job", request.Id);

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to resubmit this job.");
        }

        // Checked before completeness so an incomplete job in the wrong state reports the real
        // problem ("not deactivated") instead of a misleading list of missing fields.
        if (job.Status != JobStatus.Deactivated)
        {
            throw new BadRequestException("Only deactivated jobs can be resubmitted for review.");
        }

        // Same completeness rules as publishing, so an unfinished job never reaches the admin's
        // review queue. BadRequest, not a message-only ValidationException: the exception
        // middleware builds validation responses from the (empty) Errors list and a fixed
        // "Validation Failed" title, which would drop the list of missing fields.
        try
        {
            JobRules.EnsureContentComplete(job);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var fromStatus = job.Status;
        job.SubmitForReview();

        await _unitOfWork.JobModerationHistories.AddAsync(
            JobModerationHistory.Create(
                job.Id,
                JobModerationAction.ResubmittedForReview,
                fromStatus,
                job.Status,
                employerId,
                employerEmail),
            cancellationToken);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(employerEmail, $"Resubmitted job '{job.Title}' for review"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishAdminNotificationsAsync(job, cancellationToken);

        return Unit.Value;
    }

    // After the save, and best-effort: each PublishAsync commits on its own, and the
    // resubmission has already been recorded. A failure for one admin must not skip the rest.
    private async Task PublishAdminNotificationsAsync(Job job, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> adminUserIds;
        try
        {
            adminUserIds = await _unitOfWork.Users.GetAdminUserIdsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to load admins to notify about resubmitted job {JobId}", job.Id);
            return;
        }

        var metadata = JsonSerializer.Serialize(new { jobId = job.Id, jobTitle = job.Title });

        foreach (var adminUserId in adminUserIds)
        {
            try
            {
                await _notificationService.PublishAsync(
                    new PublishNotificationParameters(
                        adminUserId,
                        NotificationType.JobResubmittedForReview,
                        "Job Resubmitted for Review",
                        $"'{job.Title}' was resubmitted and needs a decision.",
                        metadata),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to publish in-app notification for resubmitted job {JobId} to admin {AdminUserId}",
                    job.Id,
                    adminUserId);
            }
        }
    }
}

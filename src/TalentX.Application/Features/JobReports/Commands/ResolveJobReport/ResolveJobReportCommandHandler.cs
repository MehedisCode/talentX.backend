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

namespace TalentX.Application.Features.JobReports.Commands.ResolveJobReport;

public sealed class ResolveJobReportCommandHandler : IRequestHandler<ResolveJobReportCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ResolveJobReportCommandHandler> _logger;

    public ResolveJobReportCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<ResolveJobReportCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<StatusUpdateResponseDto> Handle(ResolveJobReportCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as admin.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in as admin.");

        var report = await _unitOfWork.JobReports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new NotFoundException("JobReport", request.ReportId);

        if (report.Status != JobReportStatus.Pending)
        {
            throw new BadRequestException($"Cannot resolve report that is '{report.Status}'.");
        }

        var job = await _unitOfWork.Jobs.GetByIdAsync(report.JobId, cancellationToken)
            ?? throw new NotFoundException("Job", report.JobId);

        var fromStatus = job.Status;

        try
        {
            report.Resolve(actorUserId, request.AdminNotes);
            job.Deactivate(request.DeactivationReason, request.Details);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var reasonLabel = DeactivationReasonLabels.ToLabel(request.DeactivationReason);

        await _unitOfWork.JobModerationHistories.AddAsync(
            JobModerationHistory.Create(
                job.Id,
                JobModerationAction.Deactivated,
                fromStatus,
                job.Status,
                actorUserId,
                actorEmail,
                request.DeactivationReason,
                request.Details),
            cancellationToken);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Resolved report for job '{job.Title}' and deactivated ({reasonLabel})"),
            cancellationToken);

        await QueueEmployerEmailAsync(job, reasonLabel, request.Details, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    job.EmployerId,
                    NotificationType.JobReportResolved,
                    "Job Report Resolved",
                    $"Your job '{job.Title}' has been deactivated. Reason: {reasonLabel}.",
                    job.Id.ToString()),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish in-app notification for resolved job report {ReportId}", report.Id);
        }

        return new StatusUpdateResponseDto("Job report resolved and job deactivated.");
    }

    private async Task QueueEmployerEmailAsync(
        Job job,
        string reasonLabel,
        string details,
        CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Users.GetByUserIdAsync(job.EmployerId);
        if (employer is null)
        {
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

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Commands.ReportJob;

public sealed class ReportJobCommandHandler : IRequestHandler<ReportJobCommand, JobReportConfirmationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReportJobCommandHandler> _logger;

    public ReportJobCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ILogger<ReportJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<JobReportConfirmationDto> Handle(ReportJobCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId 
            ?? throw new UnauthorizedException("You must be signed in to report a job.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken) 
            ?? throw new NotFoundException("Job", request.JobId);

        if (job.Status != JobStatus.Published)
        {
            throw new BadRequestException("Only published jobs can be reported.");
        }

        var alreadyReported = await _unitOfWork.JobReports.ExistsForJobAndReporterAsync(request.JobId, currentUserId, cancellationToken);
        if (alreadyReported)
        {
            throw new ConflictException("You have already reported this job.");
        }

        var report = new JobReport(job.Id, currentUserId, request.Reason, request.AdditionalDetails);
        await _unitOfWork.JobReports.AddAsync(report, cancellationToken);

        var reasonLabel = JobReportReasonLabels.ToLabel(request.Reason);
        var auditLogEmail = _currentUserService.Email ?? currentUserId;
        var auditLog = new AuditLog(auditLogEmail, $"Talent reported job '{job.Title}' ({reasonLabel})");
        
        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishJobReportedNotificationsAsync(job, report, reasonLabel, cancellationToken);

        return new JobReportConfirmationDto(report.Id, "Thank you for reporting this job. Our moderation team will review it.");
    }

    private async Task PublishJobReportedNotificationsAsync(Job job, JobReport report, string reasonLabel, CancellationToken cancellationToken)
    {
        try
        {
            var adminUserIds = await _unitOfWork.Users.GetAdminUserIdsAsync(cancellationToken);
            var metadata = JsonSerializer.Serialize(new { jobId = job.Id, reportId = report.Id });

            foreach (var adminUserId in adminUserIds)
            {
                var notificationParameters = new PublishNotificationParameters(
                    adminUserId, 
                    NotificationType.JobReported, 
                    "Job Reported", 
                    $"Job '{job.Title}' was reported: {reasonLabel}", 
                    metadata);
                    
                await _notificationService.PublishAsync(notificationParameters, cancellationToken);
            }
        }
        catch (System.Exception exception)
        {
            _logger.LogError(exception, "Failed to publish job reported notifications for job {JobId}", job.Id);
        }
    }
}

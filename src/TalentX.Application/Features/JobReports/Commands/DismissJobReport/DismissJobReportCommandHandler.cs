using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Commands.DismissJobReport;

public sealed class DismissJobReportCommandHandler : IRequestHandler<DismissJobReportCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DismissJobReportCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(DismissJobReportCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as admin.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in as admin.");

        var report = await _unitOfWork.JobReports.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new NotFoundException("JobReport", request.ReportId);

        if (report.Status != JobReportStatus.Pending)
        {
            throw new BadRequestException($"Cannot dismiss report that is '{report.Status}'.");
        }

        var job = await _unitOfWork.Jobs.GetByIdAsync(report.JobId, cancellationToken);

        try
        {
            report.Dismiss(actorUserId, request.AdminNotes);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var jobTitle = job?.Title ?? report.JobId.ToString();

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Dismissed report for job '{jobTitle}'"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Job report dismissed.");
    }
}

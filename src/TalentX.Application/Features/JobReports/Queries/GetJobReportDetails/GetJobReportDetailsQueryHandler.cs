using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobReports.DTOs;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;

public sealed class GetJobReportDetailsQueryHandler : IRequestHandler<GetJobReportDetailsQuery, JobReportDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetJobReportDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobReportDetailsDto> Handle(GetJobReportDetailsQuery request, CancellationToken cancellationToken)
    {
        var report = await _unitOfWork.JobReports.GetByIdAsync(request.ReportId, cancellationToken) 
            ?? throw new NotFoundException("JobReport", request.ReportId);

        var reporter = await _unitOfWork.Users.GetByUserIdAsync(report.ReporterUserId);
        
        string? resolvedByEmail = null;
        if (report.ResolvedByUserId != null)
        {
            var resolvedBy = await _unitOfWork.Users.GetByUserIdAsync(report.ResolvedByUserId);
            resolvedByEmail = resolvedBy?.Email;
        }

        var reasonLabel = JobReportReasonLabels.ToLabel(report.Reason);

        return new JobReportDetailsDto(
            report.Id,
            report.JobId,
            report.Job.Title,
            report.Job.CompanyName,
            report.Reason,
            reasonLabel,
            report.Status,
            report.AdditionalDetails,
            report.AdminNotes,
            report.ReporterUserId,
            reporter?.Email,
            report.ResolvedByUserId,
            resolvedByEmail,
            report.ResolvedAt,
            report.CreatedAt,
            report.Job.Status);
    }
}

using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobReports.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReports;

public sealed class GetJobReportsQueryHandler : IRequestHandler<GetJobReportsQuery, PaginatedList<JobReportListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetJobReportsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<JobReportListItemDto>> Handle(GetJobReportsQuery request, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.JobReports.GetReportsAsync(
            request.Status, 
            request.SearchQuery, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        var mappedItems = new List<JobReportListItemDto>();
        var reporterEmails = new Dictionary<string, string?>();

        foreach (var report in result.Items)
        {
            if (!reporterEmails.TryGetValue(report.ReporterUserId, out var email))
            {
                var user = await _unitOfWork.Users.GetByUserIdAsync(report.ReporterUserId);
                email = user?.Email;
                reporterEmails[report.ReporterUserId] = email;
            }

            var reasonLabel = JobReportReasonLabels.ToLabel(report.Reason);

            mappedItems.Add(new JobReportListItemDto(
                report.Id,
                report.JobId,
                report.Job.Title,
                report.Job.CompanyName,
                report.Reason,
                reasonLabel,
                report.Status,
                report.CreatedAt,
                email));
        }

        return new PaginatedList<JobReportListItemDto>(mappedItems, result.Page, result.PageSize, result.TotalCount);
    }
}

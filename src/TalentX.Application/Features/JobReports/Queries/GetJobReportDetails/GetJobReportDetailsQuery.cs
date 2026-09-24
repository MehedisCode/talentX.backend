using MediatR;
using TalentX.Application.Features.JobReports.DTOs;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;

public sealed record GetJobReportDetailsQuery(Guid ReportId) : IRequest<JobReportDetailsDto>;

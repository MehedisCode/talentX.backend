using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobReports.Commands.DismissJobReport;

public sealed record DismissJobReportCommand(Guid ReportId, string? AdminNotes) : IRequest<StatusUpdateResponseDto>;

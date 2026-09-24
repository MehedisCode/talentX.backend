using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.Commands.ResolveJobReport;

public sealed record ResolveJobReportCommand(Guid ReportId, DeactivationReason DeactivationReason, string Details, string? AdminNotes) : IRequest<StatusUpdateResponseDto>;

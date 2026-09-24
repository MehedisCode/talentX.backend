using System;
using MediatR;
using TalentX.Domain.Enums;
using TalentX.Application.Features.JobReports.DTOs;

namespace TalentX.Application.Features.JobReports.Commands.ReportJob;

public sealed record ReportJobCommand(Guid JobId, JobReportReason Reason, string? AdditionalDetails) : IRequest<JobReportConfirmationDto>;

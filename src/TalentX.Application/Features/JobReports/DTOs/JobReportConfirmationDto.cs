using System;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record JobReportConfirmationDto(Guid ReportId, string Message);

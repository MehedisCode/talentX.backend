using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record ReportJobRequestDto(JobReportReason Reason, string? AdditionalDetails);

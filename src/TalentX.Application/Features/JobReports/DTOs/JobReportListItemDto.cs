using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record JobReportListItemDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    JobReportReason Reason,
    string ReasonLabel,
    JobReportStatus Status,
    DateTime CreatedAt,
    string? ReporterEmail);

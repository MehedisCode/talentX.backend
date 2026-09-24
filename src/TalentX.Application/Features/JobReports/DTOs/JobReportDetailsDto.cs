using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record JobReportDetailsDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    JobReportReason Reason,
    string ReasonLabel,
    JobReportStatus Status,
    string? AdditionalDetails,
    string? AdminNotes,
    string ReporterUserId,
    string? ReporterEmail,
    string? ResolvedByUserId,
    string? ResolvedByEmail,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    JobStatus JobStatus);

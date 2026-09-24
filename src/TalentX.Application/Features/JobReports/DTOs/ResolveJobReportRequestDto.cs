using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobReports.DTOs;

public sealed record ResolveJobReportRequestDto(DeactivationReason DeactivationReason, string Details, string? AdminNotes);

using System;
using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobReport : BaseEntity
{
    public Guid JobId { get; private set; }
    public string ReporterUserId { get; private set; } = string.Empty;
    public JobReportReason Reason { get; private set; }
    public string? AdditionalDetails { get; private set; }
    public JobReportStatus Status { get; private set; }
    public string? AdminNotes { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public Job Job { get; private set; } = null!;

    private JobReport() { }

    public JobReport(Guid jobId, string reporterUserId, JobReportReason reason, string? additionalDetails)
    {
        JobId = jobId;
        ReporterUserId = reporterUserId;
        Reason = reason;
        AdditionalDetails = additionalDetails?.Trim();
        Status = JobReportStatus.Pending;
    }

    public void Resolve(string adminUserId, string? adminNotes)
    {
        if (Status != JobReportStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reports can be resolved.");
        }

        Status = JobReportStatus.Resolved;
        ResolvedByUserId = adminUserId;
        AdminNotes = adminNotes?.Trim();
        ResolvedAt = DateTime.UtcNow;
    }

    public void Dismiss(string adminUserId, string? adminNotes)
    {
        if (Status != JobReportStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reports can be dismissed.");
        }

        Status = JobReportStatus.Dismissed;
        ResolvedByUserId = adminUserId;
        AdminNotes = adminNotes?.Trim();
        ResolvedAt = DateTime.UtcNow;
    }
}

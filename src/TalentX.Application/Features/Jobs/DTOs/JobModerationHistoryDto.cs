using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.DTOs;

public sealed record JobModerationHistoryDto
{
    public Guid Id { get; init; }
    public JobModerationAction Action { get; init; }
    public JobStatus FromStatus { get; init; }
    public JobStatus ToStatus { get; init; }
    public DeactivationReason? Reason { get; init; }

    // Sent alongside the enum so the frontend does not keep its own copy of the wording.
    public string? ReasonLabel { get; init; }

    public string? Details { get; init; }
    public string ActorEmail { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

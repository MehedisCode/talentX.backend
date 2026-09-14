namespace TalentX.Application.Features.AuditLogs.DTOs;

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public string Timestamp { get; init; } = string.Empty;
    public string Actor { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
    public string Action { get; init; } = string.Empty;
}

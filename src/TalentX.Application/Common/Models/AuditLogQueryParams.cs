namespace TalentX.Application.Common.Models;

public sealed record AuditLogQueryParams(
    string? SearchQuery,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize);

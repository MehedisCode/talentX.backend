namespace TalentX.Application.Common.Models;

public sealed record AuditLogQueryParams
{
    public string? SearchQuery { get; init; }
    public string? ActionType { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

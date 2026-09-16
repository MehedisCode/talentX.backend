using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Models;

public sealed record AdminJobQueryParams
{
    public string? SearchQuery { get; init; }
    public JobStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

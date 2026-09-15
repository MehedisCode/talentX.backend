using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Models;

public sealed record UserQueryParams
{
    public string? SearchQuery { get; init; }
    public UserRole? Role { get; init; }
    public AccountStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

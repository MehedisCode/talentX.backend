using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Users.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<PaginatedList<AdminUserListItemDto>>
{
    public string? SearchQuery { get; init; }
    public UserRole? Role { get; init; }
    public AccountStatus? Status { get; init; }
    public int Page { get; init; } = PaginationRequest.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationRequest.DefaultPageSize;
}

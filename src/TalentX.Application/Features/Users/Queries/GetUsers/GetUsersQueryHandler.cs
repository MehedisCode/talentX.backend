using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Users.DTOs;

namespace TalentX.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<AdminUserListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedList<AdminUserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var queryParams = new UserQueryParams
        {
            SearchQuery = request.SearchQuery,
            Role = request.Role,
            Status = request.Status,
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };

        var (items, totalCount) = await _unitOfWork.Users.GetPagedAsync(queryParams, cancellationToken);

        var userDtos = items.Select(user => new AdminUserListItemDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsVerified = user.IsVerified,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        }).ToList();

        return new PaginatedList<AdminUserListItemDto>(userDtos, normalizedPage, normalizedPageSize, totalCount);
    }
}

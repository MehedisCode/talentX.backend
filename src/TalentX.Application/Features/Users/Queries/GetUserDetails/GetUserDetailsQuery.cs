using MediatR;
using TalentX.Application.Features.Users.DTOs;

namespace TalentX.Application.Features.Users.Queries.GetUserDetails;

public sealed record GetUserDetailsQuery : IRequest<AdminUserDetailsDto>
{
    public Guid UserId { get; init; }
}

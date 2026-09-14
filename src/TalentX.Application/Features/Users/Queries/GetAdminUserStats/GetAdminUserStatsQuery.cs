using MediatR;
using TalentX.Application.Features.Users.DTOs;

namespace TalentX.Application.Features.Users.Queries.GetAdminUserStats;

public sealed record GetAdminUserStatsQuery : IRequest<AdminUserStatsDto>;

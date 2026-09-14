using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Users.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Users.Queries.GetAdminUserStats;

public sealed class GetAdminUserStatsQueryHandler : IRequestHandler<GetAdminUserStatsQuery, AdminUserStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminUserStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminUserStatsDto> Handle(GetAdminUserStatsQuery request, CancellationToken cancellationToken)
    {
        var (roleCounts, statusCounts) = await _unitOfWork.Users.GetRoleAndStatusCountsAsync(cancellationToken);

        var talentsCount = roleCounts.GetValueOrDefault(UserRole.Talent);
        var employersCount = roleCounts.GetValueOrDefault(UserRole.Employer);
        var guestsCount = roleCounts.GetValueOrDefault(UserRole.Guest);

        return new AdminUserStatsDto
        {
            TotalUsers = talentsCount + employersCount + guestsCount,
            TalentsCount = talentsCount,
            EmployersCount = employersCount,
            GuestsCount = guestsCount,
            DeactivatedCount = statusCounts.GetValueOrDefault(AccountStatus.Deactivated)
        };
    }
}

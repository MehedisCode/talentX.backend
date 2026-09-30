using MediatR;
using TalentX.Application.Features.AdminDashboard.DTOs;

namespace TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>
{
    // UTC instants used exactly as sent; the handler defaults to the last 30 days when both are null.
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.AdminDashboard.Queries.GetAdminDashboard;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class DashboardController : BaseApiController
{
    /// <summary>
    /// Gets platform-wide metrics, status breakdowns, a recruitment trend and recent activity for a date range.
    /// </summary>
    /// <param name="query">Optional start and end instants (UTC). Defaults to the last 30 days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The admin dashboard data for the range.</returns>
    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] GetAdminDashboardQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetAdminDashboardQuery(), cancellationToken);
        return Ok(result);
    }
}

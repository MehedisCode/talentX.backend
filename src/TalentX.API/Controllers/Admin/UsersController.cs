using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Users.Queries.GetAdminUserStats;
using TalentX.Application.Features.Users.Queries.GetUserDetails;
using TalentX.Application.Features.Users.Queries.GetUsers;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/v1/admin/users")]
public class UsersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminUserStatsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetUserDetailsQuery { UserId = id }, cancellationToken);
        return Ok(result);
    }
}

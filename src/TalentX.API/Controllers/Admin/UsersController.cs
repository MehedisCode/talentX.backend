using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;
using TalentX.Application.Features.Users.DTOs;
using TalentX.Application.Features.Users.Queries.GetAdminUserStats;
using TalentX.Application.Features.Users.Queries.GetUserDetails;
using TalentX.Application.Features.Users.Queries.GetUsers;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[Route("api/v1/admin/users")]
public class UsersController : BaseApiController
{
    /// <summary>
    /// Gets a paginated, filterable list of Talent/Employer/Guest accounts.
    /// </summary>
    /// <param name="query">Search text, role/status filters, and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of user summaries.</returns>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(PaginatedList<AdminUserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets aggregate account counts (by role and by status) for the admin dashboard.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Grouped account counts.</returns>
    [HttpGet("stats")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(AdminUserStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserStats(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminUserStatsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets full details for a single Talent/Employer/Guest account, including any
    /// role-specific profile section.
    /// </summary>
    /// <param name="id">The account's user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account's details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetUserDetailsQuery { UserId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activates or deactivates a Talent/Employer/Guest account. Writes an audit log entry and
    /// queues an account-status-changed notification email to the affected user.
    /// </summary>
    /// <param name="id">The target account's user id.</param>
    /// <param name="command">The new account status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(StatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserAccountStatus(
        Guid id,
        [FromBody] UpdateUserAccountStatusCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { UserId = id }, cancellationToken);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using TalentX.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using TalentX.Application.Features.Notifications.Queries.GetNotifications;
using TalentX.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Notifications;

[ApiController]
[Route("api/v1/notifications")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class NotificationsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetNotificationsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await Sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(count);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await Sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return NoContent();
    }
}

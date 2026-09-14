using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.API.Controllers.NotificationPreferences.Requests;
using TalentX.Application.Features.NotificationPreferences.Commands.UpdateNotificationPreferences;
using TalentX.Application.Features.NotificationPreferences.Queries.GetNotificationPreferences;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.NotificationPreferences;

[ApiController]
[Route("api/v1/notification-preferences")]
[Authorize(Roles = nameof(UserRole.Talent))]
public class NotificationPreferencesController : BaseApiController
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        var preferences = await Sender.Send(new GetNotificationPreferencesQuery(), cancellationToken);
        return Ok(preferences);
    }

    [HttpPatch]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        await Sender.Send(new UpdateNotificationPreferencesCommand(request.JobAlertsEmailEnabled), cancellationToken);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.API.Controllers.JobAlerts.Requests;
using TalentX.Application.Features.JobAlerts.Commands.CreateJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;
using TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;
using TalentX.Application.Features.JobAlerts.Queries.GetJobAlerts;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobAlerts;

[ApiController]
[Route("api/v1/job-alerts")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class JobAlertsController : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> CreateJobAlert(
        [FromBody] CreateJobAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alertId = await Sender.Send(new CreateJobAlertCommand(request.ToParameters()), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, alertId);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> GetJobAlerts(CancellationToken cancellationToken)
    {
        var alerts = await Sender.Send(new GetJobAlertsQuery(), cancellationToken);
        return Ok(alerts);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> UpdateJobAlert(
        [FromRoute] Guid id,
        [FromBody] UpdateJobAlertRequest request,
        CancellationToken cancellationToken)
    {
        await Sender.Send(new UpdateJobAlertCommand(id, request.ToParameters()), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> ToggleJobAlert(
        [FromRoute] Guid id,
        [FromBody] ToggleJobAlertRequest request,
        CancellationToken cancellationToken)
    {
        await Sender.Send(new ToggleJobAlertCommand(id, request.IsEnabled), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> DeleteJobAlert(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteJobAlertCommand(id), cancellationToken);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Jobs.Commands.DeactivateJob;
using TalentX.Application.Features.Jobs.Commands.ReactivateJob;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/jobs")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class JobsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] GetAdminJobsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query ?? new GetAdminJobsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobDetails(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminJobDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateJob(
        Guid id,
        [FromBody] DeactivateJobCommand command,
        CancellationToken cancellationToken)
    {
        // The route is the source of truth for which job; any JobId in the body is ignored.
        var result = await Sender.Send(command with { JobId = id }, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateJob(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ReactivateJobCommand(id), cancellationToken);
        return Ok(result);
    }
}

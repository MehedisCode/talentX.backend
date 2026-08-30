using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Commands.ApplyToJob;
using TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;

namespace TalentX.API.Controllers.JobApplications;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class JobApplicationsController : BaseApiController
{
    [HttpGet("{jobId:guid}/applications/me")]
    public async Task<IActionResult> GetMyApplicationStatus(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyApplicationStatusQuery(jobId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{jobId:guid}/applications")]
    public async Task<IActionResult> ApplyToJob(
        Guid jobId,
        [FromBody] ApplyToJobCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { JobId = jobId }, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

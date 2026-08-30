using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.API.Controllers;
using TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;

namespace TalentX.API.Controllers.JobApplications;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class JobApplicationsController : BaseApiController
{
    // GET /api/v1/jobs/{jobId}/applications/me
    // Talent is taken from the bearer token; returns { "hasApplied": bool }.
    // 404 when no open (Published, unexpired) job matches jobId.
    [HttpGet("{jobId:guid}/applications/me")]
    public async Task<IActionResult> GetMyApplicationStatus(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyApplicationStatusQuery(jobId), cancellationToken);
        return Ok(result);
    }
}

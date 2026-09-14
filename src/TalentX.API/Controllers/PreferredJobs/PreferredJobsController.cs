using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.PreferredJobs;

[ApiController]
[Route("api/v1/preferred-jobs")]
[Authorize(Roles = nameof(UserRole.Talent))]
public class PreferredJobsController : BaseApiController
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Talent))]
    public async Task<IActionResult> GetPreferredJobs(
        [FromQuery] PreferredJobsQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        var jobs = await Sender.Send(new GetPreferredJobsQuery(queryParams), cancellationToken);
        return Ok(jobs);
    }
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Queries;

namespace TalentX.API.Controllers.Jobs;

[Route("api/v1/jobs")]
public class JobsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPublishedJobsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await Sender.Send(new GetJobDetailsQuery(id), cancellationToken);
        return Ok(job);
    }
}

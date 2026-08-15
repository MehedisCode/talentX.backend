using MediatR;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Queries;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPublishedJobsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _mediator.Send(new GetJobDetailsQuery(id), cancellationToken);
        if (job is null)
            return NotFound(new { message = "Job not found" });
        return Ok(job);
    }
}

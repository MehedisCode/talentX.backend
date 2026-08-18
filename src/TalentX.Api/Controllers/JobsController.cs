using MediatR;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Queries;
using TalentX.Application.Jobs.Commands.CreateJob;
using TalentX.Application.Jobs.Commands.UpdateJob;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
    
    [HttpPost]
    // [Authorize(Roles = "Employer")] // Authorization will be implemented by someone else as stated by user
    public async Task<ActionResult<Guid>> CreateJob([FromBody] CreateJobCommand command)
    {
        var jobId = await _mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, jobId);
    }

    [HttpPatch("{id}")]
    // [Authorize(Roles = "Employer")] // Authorization will be implemented by someone else as stated by user
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command)
    {
        await _mediator.Send(command with { Id = id });
        return NoContent();
    }

}

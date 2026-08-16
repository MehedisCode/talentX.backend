using MediatR;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Jobs.Commands.CreateJob;
using TalentX.Application.Jobs.Commands.UpdateJob;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
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

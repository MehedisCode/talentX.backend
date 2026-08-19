using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.Queries.GetJob;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : BaseApiController
{
    [HttpPost]
    // [Authorize(Roles = "Employer")] // Authorization will be implemented by someone else as stated by user
    public async Task<ActionResult<Guid>> CreateJob([FromBody] CreateJobCommand command)
    {
        var jobId = await Sender.Send(command);
        return StatusCode(StatusCodes.Status201Created, jobId);
    }

    [HttpPatch("{id}")]
    // [Authorize(Roles = "Employer")] // Authorization will be implemented by someone else as stated by user
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command)
    {
        await Sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJob(Guid id)
    {
        var job = await Sender.Send(new GetJobQuery(id));

        if (job == null) return NotFound();

        return Ok(job);
    }
}

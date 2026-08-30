using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetMyJob;
using TalentX.Application.Features.Jobs.Queries.GetMyJobs;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : BaseApiController
{
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<Guid>> CreateJob([FromBody] CreateJobCommand command)
    {
        var jobId = await Sender.Send(command);
        return StatusCode(StatusCodes.Status201Created, jobId);
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command)
    {
        await Sender.Send(command with { Id = id });
        return NoContent();
    }
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
    
    [HttpGet("mine/{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetMyJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await Sender.Send(new GetMyJobQuery(id), cancellationToken);
        return Ok(job);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetMyJobs(CancellationToken cancellationToken)
    {
        var jobs = await Sender.Send(new GetMyJobsQuery(), cancellationToken);
        return Ok(jobs);
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetMyJob;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;
using TalentX.Domain.Enums;
using TalentX.Application.Features.JobApplications.Commands.Apply;

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
        [FromQuery] string? keyword = null,
        [FromQuery] string? location = null,
        [FromQuery] EmploymentType? employmentType = null,
        [FromQuery] ExperienceLevel? experienceLevel = null,
        [FromQuery] WorkArrangement? workArrangement = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublishedJobsQuery(
            Keyword: keyword,
            Location: location,
            EmploymentType: employmentType,
            ExperienceLevel: experienceLevel,
            WorkArrangement: workArrangement,
            Page: page,
            PageSize: pageSize);

        var result = await Sender.Send(query, cancellationToken);
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

    [HttpPost("{id:guid}/apply")]
    [AllowAnonymous]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyToJobCommand command)
    {
        var response = await Sender.Send(command with { JobId = id });
        return Ok(response);
    }
}
